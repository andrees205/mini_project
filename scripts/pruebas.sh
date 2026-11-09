#!/usr/bin/env bash
set -uo pipefail

BASE="${BASE:-http://localhost:5131}"
OUT="${OUT:-docs/evidencias}"
mkdir -p "$OUT"
rm -f "$OUT"/*.txt
RESUMEN="$OUT/RESUMEN.md"

json() { python3 -c 'import sys,json
raw=sys.stdin.read()
try: print(json.dumps(json.loads(raw), indent=2, ensure_ascii=False))
except Exception: print(raw)'; }

campo() { python3 -c "import sys,json; print(json.load(sys.stdin)$1)"; }

login() { curl -s -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' \
            -d "{\"nombreUsuario\":\"$1\",\"password\":\"$2\"}" | campo '["token"]'; }

PASADAS=0; FALLIDAS=0
printf '# Resultados de las pruebas\n\nGenerado por `scripts/pruebas.sh` el %s contra `%s`.\n\n' \
  "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$BASE" > "$RESUMEN"
printf '| # | Prueba | Esperado | Obtenido | Resultado | Evidencia |\n|---|---|---|---|---|---|\n' >> "$RESUMEN"

prueba() {
  local num=$1 slug=$2 desc=$3 esperado=$4 quien=$5 metodo=$6 ruta=$7 token=${8:-} body=${9:-}
  local archivo="$OUT/$(printf '%02d' "$num")-$slug.txt"
  local args=(-s -i -X "$metodo" "$BASE$ruta")
  local mostrado="curl -X $metodo $BASE$ruta"
  if [[ -n "$token" ]]; then
    args+=(-H "Authorization: Bearer $token"); mostrado+=" \\\\\n  -H 'Authorization: Bearer <token de $quien>'"
  fi
  if [[ -n "$body" ]]; then
    args+=(-H 'Content-Type: application/json' -d "$body"); mostrado+=" \\\\\n  -H 'Content-Type: application/json' \\\\\n  -d '$body'"
  fi

  local respuesta; respuesta=$(curl "${args[@]}")
  local cabecera cuerpo status
  cabecera=$(printf '%s' "$respuesta" | sed '/^\r\{0,1\}$/q' | tr -d '\r')
  cuerpo=$(printf '%s' "$respuesta" | sed '1,/^\r\{0,1\}$/d')
  status=$(printf '%s' "$cabecera" | head -1 | awk '{print $2}')

  local resultado="OK"
  if [[ ! "$status" =~ ^($esperado)$ ]]; then resultado="FALLA"; FALLIDAS=$((FALLIDAS+1)); else PASADAS=$((PASADAS+1)); fi

  {
    echo "PRUEBA $num — $desc"
    echo "Actor: $quien    Esperado: $esperado    Obtenido: $status    $resultado"
    echo
    echo "### Petición"
    echo -e "$mostrado"
    echo
    echo "### Respuesta"
    printf '%s\n' "$cabecera" | grep -iE '^(HTTP/|content-type|location)'
    echo
    if [[ -n "$cuerpo" ]]; then printf '%s' "$cuerpo" | json; else echo "(sin cuerpo)"; fi
  } > "$archivo"

  printf '| %s | %s | %s | %s | %s | [%s](%s) |\n' "$num" "$desc" "${esperado//|/ o }" "$status" "$resultado" \
    "$(basename "$archivo")" "$(basename "$archivo")" >> "$RESUMEN"
  printf '%-3s %-70s esperado %-8s obtenido %-4s %s\n' "$num" "$desc" "$esperado" "$status" "$resultado"
  ULTIMO_CUERPO=$cuerpo
}

echo "== Obteniendo tokens =="
ADMIN=$(login admin 'Admin123!')
TEC1=$(login tecnico1 'Tecnico123!')
TEC2=$(login tecnico2 'Tecnico123!')
USU1=$(login usuario1 'Usuario123!')
USU2=$(login usuario2 'Usuario123!')
[[ -n "$ADMIN" && -n "$USU1" ]] || { echo "No se pudo hacer login. ¿Está la API corriendo en $BASE?"; exit 1; }

echo "== Tabla de pruebas (sección 6) =="
prueba 1 login-correcto "Login con credenciales correctas" 200 anónimo POST /api/auth/login "" \
  '{"nombreUsuario":"usuario1","password":"Usuario123!"}'
prueba 2 login-password-incorrecta "Login con contraseña incorrecta" 401 anónimo POST /api/auth/login "" \
  '{"nombreUsuario":"usuario1","password":"incorrecta"}'
prueba 3 listar-sin-jwt "Consultar incidentes sin JWT" 401 anónimo GET /api/incidentes
prueba 6 crear-correcto "Crear incidente correctamente" 201 usuario1 POST /api/incidentes "$USU1" \
  '{"titulo":"Impresora no imprime","descripcion":"La impresora del piso 2 no responde","prioridad":"Alta"}'
ID_U1=$(printf '%s' "$ULTIMO_CUERPO" | campo '["id"]')
prueba 4 tecnico-elimina "Técnico intenta eliminar un incidente" 403 tecnico1 DELETE "/api/incidentes/$ID_U1" "$TEC1"
prueba 5 crear-sin-titulo "Crear incidente sin título" 400 usuario1 POST /api/incidentes "$USU1" \
  '{"descripcion":"No funciona el correo","prioridad":"Media"}'

ID_U2=$(curl -s -X POST "$BASE/api/incidentes" -H "Authorization: Bearer $USU2" -H 'Content-Type: application/json' \
  -d '{"titulo":"Correo no sincroniza","descripcion":"Outlook no descarga correos","prioridad":"Media"}' | campo '["id"]')
prueba 7 usuario-ve-ajeno "Usuario consulta incidente de otro" 404 usuario1 GET "/api/incidentes/$ID_U2" "$USU1"
prueba 8 cerrar-sin-resolver "Cerrar un incidente que no está Resuelto (Pendiente)" 400 admin PUT \
  "/api/incidentes/$ID_U1/estado" "$ADMIN" '{"nuevoEstado":"Cerrado"}'
prueba 9 excepcion-500 "La API produce una excepción (endpoint temporal)" 500 anónimo GET /api/debug/error

echo "== Casos críticos y reglas adicionales =="
prueba 10 login-usuario-inexistente "Login con usuario inexistente (mismo mensaje que #2)" 401 anónimo POST /api/auth/login "" \
  '{"nombreUsuario":"noexiste","password":"loquesea"}'
prueba 11 asignar-no-tecnico "Asignar a un usuario que no es Técnico (caso 3)" 400 admin PUT \
  "/api/incidentes/$ID_U1/asignar" "$ADMIN" '{"tecnicoId":4}'
prueba 12 usuario-asigna "Usuario intenta asignar técnico" 403 usuario1 PUT \
  "/api/incidentes/$ID_U1/asignar" "$USU1" '{"tecnicoId":2}'
prueba 13 asignar-tecnico "Admin asigna a tecnico1 (Pendiente → Asignado)" 200 admin PUT \
  "/api/incidentes/$ID_U1/asignar" "$ADMIN" '{"tecnicoId":2}'
prueba 14 tecnico-modifica-ajeno "tecnico2 cambia estado de incidente de tecnico1 (caso 1)" 404 tecnico2 PUT \
  "/api/incidentes/$ID_U1/estado" "$TEC2" '{"nuevoEstado":"EnProceso"}'
prueba 15 tecnico-ve-ajeno "tecnico2 consulta incidente asignado a tecnico1 (caso 1)" 404 tecnico2 GET \
  "/api/incidentes/$ID_U1" "$TEC2"
prueba 16 saltar-paso "Saltar pasos: Asignado → Resuelto (caso 3)" 400 tecnico1 PUT \
  "/api/incidentes/$ID_U1/estado" "$TEC1" '{"nuevoEstado":"Resuelto"}'
prueba 17 en-proceso "tecnico1 avanza Asignado → EnProceso" 200 tecnico1 PUT \
  "/api/incidentes/$ID_U1/estado" "$TEC1" '{"nuevoEstado":"EnProceso"}'
prueba 18 resuelto "tecnico1 avanza EnProceso → Resuelto (se llena FechaResolucion)" 200 tecnico1 PUT \
  "/api/incidentes/$ID_U1/estado" "$TEC1" '{"nuevoEstado":"Resuelto"}'
prueba 19 cerrado "tecnico1 avanza Resuelto → Cerrado" 200 tecnico1 PUT \
  "/api/incidentes/$ID_U1/estado" "$TEC1" '{"nuevoEstado":"Cerrado"}'
prueba 20 reabrir-cerrado "Reabrir un incidente Cerrado" 400 admin PUT \
  "/api/incidentes/$ID_U1/estado" "$ADMIN" '{"nuevoEstado":"EnProceso"}'
prueba 21 eliminar-cerrado "Admin elimina un incidente Cerrado (decisión A3)" 400 admin DELETE \
  "/api/incidentes/$ID_U1" "$ADMIN"
prueba 22 listar-tecnico "tecnico1 lista: solo ve sus asignados" 200 tecnico1 GET /api/incidentes "$TEC1"
prueba 23 listar-usuario "usuario2 lista: solo ve los que creó" 200 usuario2 GET /api/incidentes "$USU2"
prueba 24 listar-admin "admin lista: ve todos" 200 admin GET /api/incidentes "$ADMIN"
prueba 25 admin-elimina "Admin elimina un incidente Pendiente" 204 admin DELETE "/api/incidentes/$ID_U2" "$ADMIN"
prueba 26 token-falso "Token manipulado/firma inválida" 401 "atacante" GET /api/incidentes "${USU1%?}x"

printf '\n**Total:** %d OK, %d con falla.\n' "$PASADAS" "$FALLIDAS" >> "$RESUMEN"
echo
echo "Resultado: $PASADAS OK, $FALLIDAS con falla. Evidencia en $OUT/"
[[ $FALLIDAS -eq 0 ]]
