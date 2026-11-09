# Resultados de las pruebas

Generado por `scripts/pruebas.sh` el 2026-11-09T23:21:13Z contra `http://localhost:5131`.

| # | Prueba | Esperado | Obtenido | Resultado | Evidencia |
|---|---|---|---|---|---|
| 1 | Login con credenciales correctas | 200 | 200 | OK | [01-login-correcto.txt](01-login-correcto.txt) |
| 2 | Login con contraseña incorrecta | 401 | 401 | OK | [02-login-password-incorrecta.txt](02-login-password-incorrecta.txt) |
| 3 | Consultar incidentes sin JWT | 401 | 401 | OK | [03-listar-sin-jwt.txt](03-listar-sin-jwt.txt) |
| 6 | Crear incidente correctamente | 201 | 201 | OK | [06-crear-correcto.txt](06-crear-correcto.txt) |
| 4 | Técnico intenta eliminar un incidente | 403 | 403 | OK | [04-tecnico-elimina.txt](04-tecnico-elimina.txt) |
| 5 | Crear incidente sin título | 400 | 400 | OK | [05-crear-sin-titulo.txt](05-crear-sin-titulo.txt) |
| 7 | Usuario consulta incidente de otro | 404 | 404 | OK | [07-usuario-ve-ajeno.txt](07-usuario-ve-ajeno.txt) |
| 8 | Cerrar un incidente que no está Resuelto (Pendiente) | 400 | 400 | OK | [08-cerrar-sin-resolver.txt](08-cerrar-sin-resolver.txt) |
| 9 | La API produce una excepción (endpoint temporal) | 500 | 500 | OK | [09-excepcion-500.txt](09-excepcion-500.txt) |
| 10 | Login con usuario inexistente (mismo mensaje que #2) | 401 | 401 | OK | [10-login-usuario-inexistente.txt](10-login-usuario-inexistente.txt) |
| 11 | Asignar a un usuario que no es Técnico (caso 3) | 400 | 400 | OK | [11-asignar-no-tecnico.txt](11-asignar-no-tecnico.txt) |
| 12 | Usuario intenta asignar técnico | 403 | 403 | OK | [12-usuario-asigna.txt](12-usuario-asigna.txt) |
| 13 | Admin asigna a tecnico1 (Pendiente → Asignado) | 200 | 200 | OK | [13-asignar-tecnico.txt](13-asignar-tecnico.txt) |
| 14 | tecnico2 cambia estado de incidente de tecnico1 (caso 1) | 404 | 404 | OK | [14-tecnico-modifica-ajeno.txt](14-tecnico-modifica-ajeno.txt) |
| 15 | tecnico2 consulta incidente asignado a tecnico1 (caso 1) | 404 | 404 | OK | [15-tecnico-ve-ajeno.txt](15-tecnico-ve-ajeno.txt) |
| 16 | Saltar pasos: Asignado → Resuelto (caso 3) | 400 | 400 | OK | [16-saltar-paso.txt](16-saltar-paso.txt) |
| 17 | tecnico1 avanza Asignado → EnProceso | 200 | 200 | OK | [17-en-proceso.txt](17-en-proceso.txt) |
| 18 | tecnico1 avanza EnProceso → Resuelto (se llena FechaResolucion) | 200 | 200 | OK | [18-resuelto.txt](18-resuelto.txt) |
| 19 | tecnico1 avanza Resuelto → Cerrado | 200 | 200 | OK | [19-cerrado.txt](19-cerrado.txt) |
| 20 | Reabrir un incidente Cerrado | 400 | 400 | OK | [20-reabrir-cerrado.txt](20-reabrir-cerrado.txt) |
| 21 | Admin elimina un incidente Cerrado (decisión A3) | 400 | 400 | OK | [21-eliminar-cerrado.txt](21-eliminar-cerrado.txt) |
| 22 | tecnico1 lista: solo ve sus asignados | 200 | 200 | OK | [22-listar-tecnico.txt](22-listar-tecnico.txt) |
| 23 | usuario2 lista: solo ve los que creó | 200 | 200 | OK | [23-listar-usuario.txt](23-listar-usuario.txt) |
| 24 | admin lista: ve todos | 200 | 200 | OK | [24-listar-admin.txt](24-listar-admin.txt) |
| 25 | Admin elimina un incidente Pendiente | 204 | 204 | OK | [25-admin-elimina.txt](25-admin-elimina.txt) |
| 26 | Token manipulado/firma inválida | 401 | 401 | OK | [26-token-falso.txt](26-token-falso.txt) |

**Total:** 26 OK, 0 con falla.
