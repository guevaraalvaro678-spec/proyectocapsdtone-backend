// Pruebas de aceptación (HU-07, HU-08, HU-12) y tiempos de respuesta contra la API local.
// Uso: node aceptacion.mjs <baseUrl> <salida.json>
import { writeFileSync } from 'node:fs';

const [BASE, SALIDA] = process.argv.slice(2);
const resultados = [];
const tiempos = [];
const marca = Date.now().toString().slice(-6);

const pdf = () => new Blob(['%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF'], { type: 'application/pdf' });

async function llamar(metodo, ruta, { token, json, form } = {}) {
  const headers = {};
  if (token) headers.Authorization = `Bearer ${token}`;
  let body;
  if (json) { headers['Content-Type'] = 'application/json'; body = JSON.stringify(json); }
  if (form) body = form;
  const inicio = performance.now();
  const r = await fetch(`${BASE}${ruta}`, { method: metodo, headers, body });
  const ms = performance.now() - inicio;
  let datos = null;
  const texto = await r.text();
  try { datos = texto ? JSON.parse(texto) : null; } catch { datos = texto; }
  return { status: r.status, datos, ms };
}

function registrar(hu, ca, descripcion, ok, detalle) {
  resultados.push({ hu, ca, descripcion, resultado: ok ? 'Aprobado' : 'Observado', detalle });
  console.log(`${ok ? '[APROBADO] ' : '[OBSERVADO]'} ${hu} ${ca} - ${descripcion} -> ${detalle}`);
}

function formulario(campos, archivo) {
  const f = new FormData();
  for (const [k, v] of Object.entries(campos)) if (v !== undefined) f.append(k, String(v));
  if (archivo) f.append('cv', archivo.blob, archivo.nombre);
  return f;
}

async function esperarApi() {
  for (let i = 0; i < 60; i++) {
    try { const r = await fetch(`${BASE}/catalogo/areas`); if (r.ok) return; } catch {}
    await new Promise((res) => setTimeout(res, 1000));
  }
  throw new Error('La API no respondió');
}

await esperarApi();

const areasResp = await llamar('GET', '/catalogo/areas');
const areas = Array.isArray(areasResp.datos) ? areasResp.datos : [];
const area = typeof areas[0] === 'string' ? areas[0] : (areas[0]?.valor ?? areas[0]?.nombre ?? areas[0]?.id);

const dni = `7${marca.padStart(7, '0')}`;
const practicante = {
  Tipo: 'Practicante', Dni: dni, Nombres: 'Lucía', Apellidos: 'Pérez Rojas', FechaNacimiento: '2003-05-10',
  Direccion: 'Av. Arequipa 1234', Distrito: 'Lince', Provincia: 'Lima', Telefono: '987654321',
  Universidad: 'Universidad Privada del Norte', Carrera: 'Ingeniería de Sistemas', Ciclo: '7', Departamento: area,
  Correo: `practicante.${marca}@prueba.local`, Password: 'Prueba2026!', AceptaTerminos: true,
};

// ---------- HU-07 ----------
const vol = await llamar('POST', '/postulacion/completa', { form: formulario({
  Tipo: 'Voluntario', Nombres: 'Mario', Apellidos: 'Quispe Lara', Telefono: '912345678',
  Correo: `voluntario.${marca}@prueba.local`, Password: 'Prueba2026!', AceptaTerminos: true,
}) });
registrar('HU-07', 'CA1', 'Cada perfil pide sus propios campos y se ofrece el selector de área',
  [200, 201].includes(vol.status) && vol.datos?.postulacion?.dni == null && areas.length > 0,
  `voluntario registrado solo con nombres, apellidos y celular (HTTP ${vol.status}); ${areas.length} áreas en el selector`);

const prac = await llamar('POST', '/postulacion/completa', { form: formulario(practicante, { blob: pdf(), nombre: 'mi curriculum.pdf' }) });
const p = prac.datos?.postulacion;
const nombreEsperado = p ? `CV_${p.codigo}_PEREZ_ROJAS.pdf` : '';
registrar('HU-07', 'CA2', 'Postulación completa con CV en PDF queda "Recibida" y el CV se renombra',
  [200, 201].includes(prac.status) && p?.estado === 'Recibida' && /^POS-\d{4}-\d{3,}$/.test(p?.codigo ?? '') && p?.cvNombre === nombreEsperado,
  `HTTP ${prac.status}, estado ${p?.estado}, código ${p?.codigo}, CV "${p?.cvNombre}"`);

const noPdf = await llamar('POST', '/postulacion/completa', { form: formulario({ ...practicante, Dni: `6${marca.padStart(7, '0')}`, Correo: `nopdf.${marca}@prueba.local` },
  { blob: new Blob(['texto'], { type: 'application/msword' }), nombre: 'cv.docx' }) });
const grande = await llamar('POST', '/postulacion/completa', { form: formulario({ ...practicante, Dni: `5${marca.padStart(7, '0')}`, Correo: `grande.${marca}@prueba.local` },
  { blob: new Blob([new Uint8Array(10 * 1024 * 1024 + 1)], { type: 'application/pdf' }), nombre: 'cv.pdf' }) });
registrar('HU-07', 'CA3', 'Se rechaza el CV que no es PDF o supera 10 MB, indicando el motivo',
  noPdf.status === 400 && grande.status === 400,
  `no PDF: HTTP ${noPdf.status} "${noPdf.datos?.mensaje}"; 10 MB + 1 byte: HTTP ${grande.status} "${grande.datos?.mensaje}"`);

// ---------- HU-08 ----------
const login = await llamar('POST', '/admin/login', { json: { usuario: 'admin', password: '123' } });
const tokenAdmin = login.datos?.sesion?.token;
const bandeja = await llamar('GET', '/postulacion', { token: tokenAdmin });
const lista = Array.isArray(bandeja.datos) ? bandeja.datos : [];
const ordenada = lista.every((x, i) => i === 0 || new Date(lista[i - 1].fechaEnvio) >= new Date(x.fechaEnvio));
registrar('HU-08', 'CA1', 'La bandeja muestra tipo y estado, ordenada por fecha de postulación',
  bandeja.status === 200 && lista.length >= 2 && lista.every((x) => x.tipo && x.estado) && ordenada,
  `HTTP ${bandeja.status}, ${lista.length} postulaciones, ordenadas de la más reciente a la más antigua: ${ordenada ? 'sí' : 'no'}`);

const cambio = await llamar('PUT', `/postulacion/${p?.id}/estado`, { token: tokenAdmin, json: { estado: 'HabilitadaEntrevista' } });
const detalle = await llamar('GET', `/postulacion/${p?.id}`, { token: tokenAdmin });
const ultimo = (detalle.datos?.historial ?? []).at(-1) ?? (detalle.datos?.historial ?? [])[0];
const conHistorial = (detalle.datos?.historial ?? []).find((h) => h.estado === 'HabilitadaEntrevista');
registrar('HU-08', 'CA2', 'El cambio de estado se guarda con fecha, hora y responsable',
  cambio.status === 200 && detalle.datos?.estado === 'HabilitadaEntrevista' && !!conHistorial?.fecha && !!conHistorial?.responsable,
  `HTTP ${cambio.status}, estado ${detalle.datos?.estado}, historial: ${conHistorial?.fecha} por "${conHistorial?.responsable}"`);

const tokenPostulante = prac.datos?.sesion?.token;
const sinRol = await llamar('GET', '/postulacion', { token: tokenPostulante });
const sinToken = await llamar('GET', '/postulacion');
registrar('HU-08', 'CA3', 'Un usuario sin rol de gestión no puede abrir la bandeja',
  sinRol.status === 403 && sinToken.status === 401, `postulante: HTTP ${sinRol.status}; sin sesión: HTTP ${sinToken.status}`);

// ---------- HU-12 ----------
const mismoCorreo = await llamar('POST', '/postulacion/completa', { form: formulario({ ...practicante, Dni: `4${marca.padStart(7, '0')}` }, { blob: pdf(), nombre: 'cv.pdf' }) });
const mismoDni = await llamar('POST', '/postulacion/completa', { form: formulario({ ...practicante, Correo: `otro.${marca}@prueba.local` }, { blob: pdf(), nombre: 'cv.pdf' }) });
registrar('HU-12', 'CA1', 'Con correo o DNI ya registrado no se crea una segunda solicitud',
  mismoCorreo.status >= 400 && mismoDni.status >= 400,
  `mismo correo: HTTP ${mismoCorreo.status} "${mismoCorreo.datos?.mensaje}"; mismo DNI: HTTP ${mismoDni.status} "${mismoDni.datos?.mensaje}"`);

const invalido = await llamar('POST', '/postulacion/completa', { form: formulario({
  ...practicante, Nombres: '', Telefono: '12345', Correo: 'no-es-correo', Dni: `3${marca.padStart(7, '0')}` }, { blob: pdf(), nombre: 'cv.pdf' }) });
const campos = Object.keys(invalido.datos?.errors ?? {});
registrar('HU-12', 'CA2', 'Campos vacíos o con formato inválido se señalan y no se registra',
  invalido.status === 400 && campos.length >= 3, `HTTP ${invalido.status}, campos señalados: ${campos.join(', ')}`);

const busqueda = await llamar('GET', `/postulacion?buscar=${dni}`, { token: tokenAdmin });
const conDni = (Array.isArray(busqueda.datos) ? busqueda.datos : []).filter((x) => x.dni === dni);
registrar('HU-12', 'CA3', 'Con datos válidos se guarda un único registro del postulante',
  conDni.length === 1, `registros con el DNI ${dni}: ${conDni.length}`);

// ---------- Tiempos de respuesta ----------
const pantallas = [
  ['Inicio de sesión', () => llamar('POST', '/admin/login', { json: { usuario: 'admin', password: '123' } })],
  ['Bandeja de postulaciones', () => llamar('GET', '/postulacion', { token: tokenAdmin })],
  ['Detalle de postulación', () => llamar('GET', `/postulacion/${p?.id}`, { token: tokenAdmin })],
  ['Dashboard', () => llamar('GET', '/dashboard/ong', { token: tokenAdmin })],
  ['Portal público (eventos)', () => llamar('GET', '/evento')],
  ['Portal público (sitio)', () => llamar('GET', '/sitio')],
];
for (const [nombre, fn] of pantallas) {
  await fn();
  const ms = [];
  for (let i = 0; i < 20; i++) ms.push((await fn()).ms);
  ms.sort((a, b) => a - b);
  const prom = ms.reduce((a, b) => a + b, 0) / ms.length;
  tiempos.push({ pantalla: nombre, promedioMs: +prom.toFixed(1), p95Ms: +ms[Math.floor(ms.length * 0.95) - 1].toFixed(1), maxMs: +ms.at(-1).toFixed(1) });
}
console.table(tiempos);

const aprobados = resultados.filter((r) => r.resultado === 'Aprobado').length;
console.log(`\nCriterios de aceptación: ${resultados.length}, aprobados: ${aprobados}, observados: ${resultados.length - aprobados}`);
writeFileSync(SALIDA, JSON.stringify({ fecha: new Date().toISOString(), resultados, tiempos }, null, 2));
