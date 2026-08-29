# ?? Guía Rápida: Crear Subdominios en tpqsolutions.com.ar

**Tiempo estimado:** 2 minutos por cliente  
**Requisitos:** cloudflared instalado en `C:\cloudflared\`

---

## ? Comandos Rápidos

### ?? Paso 1: Autenticarse (Solo una vez)

```powershell
# Ejecutar esto UNA SOLA VEZ en tu máquina
C:\cloudflared\cloudflared.exe tunnel login
```

**Qué hace:**
- Abre el navegador
- Inicias sesión con: `manuclaro@gmail.com`
- Seleccionas el dominio: `tpqsolutions.com.ar`
- Guarda un certificado en: `C:\Users\Manuel\.cloudflared\cert.pem`

**? Ya tienes acceso para crear subdominios ilimitados**

---

### ?? Paso 2: Crear Subdominio para un Cliente

**Ejemplo: Cliente "Victor"**

```powershell
# 2.1 - Crear el tunnel
C:\cloudflared\cloudflared.exe tunnel create sqlbridge-victor

# 2.2 - Crear el subdominio (ESTO GENERA EL SUBDOMINIO)
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-victor victor.tpqsolutions.com.ar
```

**? ¡Listo! El subdominio `victor.tpqsolutions.com.ar` ya existe**

---

### ?? Paso 3: Verificar

```powershell
# Ver todos los tunnels
C:\cloudflared\cloudflared.exe tunnel list

# Ver info de un tunnel específico
C:\cloudflared\cloudflared.exe tunnel info sqlbridge-victor
```

---

## ?? Ejemplos para Múltiples Clientes

### Cliente 1: Victor

```powershell
C:\cloudflared\cloudflared.exe tunnel create sqlbridge-victor
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-victor victor.tpqsolutions.com.ar
```

? **Resultado:** `victor.tpqsolutions.com.ar`

---

### Cliente 2: Pepe

```powershell
C:\cloudflared\cloudflared.exe tunnel create sqlbridge-pepe
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-pepe pepe.tpqsolutions.com.ar
```

? **Resultado:** `pepe.tpqsolutions.com.ar`

---

### Cliente 3: ABC

```powershell
C:\cloudflared\cloudflared.exe tunnel create sqlbridge-abc
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-abc abc.tpqsolutions.com.ar
```

? **Resultado:** `abc.tpqsolutions.com.ar`

---

## ?? Verificar en Cloudflare

### Opción 1: Dashboard Web

1. Ve a: https://dash.cloudflare.com
2. Inicia sesión con: `manuclaro@gmail.com`
3. Selecciona: **tpqsolutions.com.ar**
4. Ve a: **DNS** ? **Records**

**Deberías ver:**

```
??????????????????????????????????????????????????????????????????????
? Type  ? Name   ? Content                              ? Status     ?
??????????????????????????????????????????????????????????????????????
? CNAME ? victor ? 12345678-abcd.cfargotunnel.com       ? Proxied    ?
? CNAME ? pepe   ? 87654321-efgh.cfargotunnel.com       ? Proxied    ?
? CNAME ? abc    ? abcdefgh-ijkl.cfargotunnel.com       ? Proxied    ?
??????????????????????????????????????????????????????????????????????
```

### Opción 2: Línea de Comandos

```powershell
# Listar tunnels
C:\cloudflared\cloudflared.exe tunnel list

# Resultado esperado:
# ID                                   NAME                CREATED
# aaaa-bbbb-cccc-dddd                 sqlbridge-victor    2026-02-10T10:00:00Z
# eeee-ffff-gggg-hhhh                 sqlbridge-pepe      2026-02-10T10:05:00Z
# iiii-jjjj-kkkk-llll                 sqlbridge-abc       2026-02-10T10:10:00Z
```

---

## ?? FAQ - Preguntas Frecuentes

### ? ¿Tengo que crear el subdominio manualmente en Cloudflare?

**NO.** El comando `tunnel route dns` lo crea automáticamente.

---

### ? ¿Cuántos subdominios puedo crear?

**Ilimitados.** El plan Free de Cloudflare permite tunnels y subdominios ilimitados.

---

### ? ¿Cuánto cuesta cada subdominio?

**$0.** Es completamente gratis.

---

### ? ¿Puedo eliminar un subdominio?

**Sí.**

```powershell
# Eliminar la ruta DNS
C:\cloudflared\cloudflared.exe tunnel route dns delete sqlbridge-victor

# Eliminar el tunnel
C:\cloudflared\cloudflared.exe tunnel delete sqlbridge-victor
```

**Nota:** También se eliminará automáticamente el registro DNS en Cloudflare.

---

### ? ¿El subdominio funciona inmediatamente?

**Sí.** La propagación DNS es instantánea porque Cloudflare gestiona tanto el DNS como el tunnel.

---

### ? ¿Puedo usar el mismo tunnel para múltiples subdominios?

**Sí, pero no es recomendable.** Lo ideal es un tunnel por cliente para aislamiento.

**Ejemplo (NO recomendado):**
```powershell
# Un solo tunnel para múltiples subdominios
C:\cloudflared\cloudflared.exe tunnel create sqlbridge-multicliente
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-multicliente victor.tpqsolutions.com.ar
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-multicliente pepe.tpqsolutions.com.ar
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-multicliente abc.tpqsolutions.com.ar
```

? **Problema:** Si cae el tunnel, caen todos los clientes.

? **Mejor:** Un tunnel independiente por cliente.

---

## ?? Conceptos Clave

### ¿Qué hace `tunnel create`?

```powershell
C:\cloudflared\cloudflared.exe tunnel create sqlbridge-victor
```

**Crea:**
1. Un **tunnel ID** único (ej: `aaaa-bbbb-cccc-dddd`)
2. Un archivo de **credenciales** en: `C:\Users\Manuel\.cloudflared\aaaa-bbbb-cccc-dddd.json`
3. Registra el tunnel en tu cuenta de Cloudflare

**NO crea el subdominio todavía.**

---

### ¿Qué hace `tunnel route dns`?

```powershell
C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-victor victor.tpqsolutions.com.ar
```

**Crea:**
1. Un registro **CNAME** en Cloudflare DNS
2. **Asocia** el subdominio `victor.tpqsolutions.com.ar` al tunnel `sqlbridge-victor`

**? AQUÍ ES DONDE SE GENERA EL SUBDOMINIO.**

---

## ?? Diagrama del Proceso

```
Tu Máquina (Ejecutas comandos)
        ?
        ? cloudflared.exe tunnel create sqlbridge-victor
        ?
?????????????????????????????????????????????????
?         Cloudflare API                        ?
?  - Crea tunnel ID: aaaa-bbbb-cccc             ?
?  - Genera credenciales.json                   ?
?????????????????????????????????????????????????
        ?
        ? cloudflared.exe tunnel route dns sqlbridge-victor victor.tpqsolutions.com.ar
        ?
?????????????????????????????????????????????????
?     Cloudflare DNS (tpqsolutions.com.ar)      ?
?  - Crea CNAME: victor ? aaaa-bbbb.cfargo...   ?
?  - Asocia subdominio al tunnel                ?
?????????????????????????????????????????????????
        ?
        ?
? Subdominio creado: victor.tpqsolutions.com.ar
```

---

## ??? Comandos de Administración

### Ver todos los tunnels

```powershell
C:\cloudflared\cloudflared.exe tunnel list
```

### Ver info de un tunnel específico

```powershell
C:\cloudflared\cloudflared.exe tunnel info sqlbridge-victor
```

### Ver rutas DNS de un tunnel

```powershell
# Cloudflare CLI (requiere instalación adicional)
# O verlo en el dashboard web
```

### Eliminar un subdominio (desasociar DNS)

```powershell
# Esto NO elimina el tunnel, solo desasocia el DNS
C:\cloudflared\cloudflared.exe tunnel route dns delete sqlbridge-victor victor.tpqsolutions.com.ar
```

### Eliminar un tunnel completo

```powershell
# Primero desasociar DNS (si lo tiene)
C:\cloudflared\cloudflared.exe tunnel route dns delete sqlbridge-victor victor.tpqsolutions.com.ar

# Luego eliminar el tunnel
C:\cloudflared\cloudflared.exe tunnel delete sqlbridge-victor
```

---

## ?? Checklist: Crear Subdominio para Nuevo Cliente

```
Cliente: _________________
Subdominio deseado: _____________.tpqsolutions.com.ar

[ ] Paso 1: Autenticado en Cloudflare (solo primera vez)
    Comando: C:\cloudflared\cloudflared.exe tunnel login

[ ] Paso 2: Crear tunnel
    Comando: C:\cloudflared\cloudflared.exe tunnel create sqlbridge-_______
    Tunnel ID obtenido: _____________________

[ ] Paso 3: Crear subdominio
    Comando: C:\cloudflared\cloudflared.exe tunnel route dns sqlbridge-_______ _______.tpqsolutions.com.ar
    Respuesta: Successfully routed...

[ ] Paso 4: Verificar en dashboard
    URL: https://dash.cloudflare.com
    Dominio: tpqsolutions.com.ar
    DNS Records: CNAME _______ ? ________.cfargotunnel.com ?

[ ] Paso 5: Anotar información
    Tunnel Name: sqlbridge-_______
    Tunnel ID: _____________________
    Subdominio: _______.tpqsolutions.com.ar
    Archivo credenciales: C:\Users\Manuel\.cloudflared\____________.json

? Subdominio creado exitosamente
```

---

## ?? Documentación Relacionada

- **Guía completa de instalación:** `docs/GUIA_INSTALACION_CLIENTE_NUEVO.md`
- **Arquitectura multi-cliente:** `docs/ARQUITECTURA_MULTICLIENTE.md`
- **Script automático:** `docs/scripts/crear-subdominios-ejemplo.ps1`

---

## ?? Tip Final

**Convención de nombres sugerida:**

```
Tunnel Name:  sqlbridge-{nombre-corto}
Subdominio:   {nombre-corto}.tpqsolutions.com.ar

Ejemplos:
- sqlbridge-victor      ? victor.tpqsolutions.com.ar
- sqlbridge-pepe        ? pepe.tpqsolutions.com.ar
- sqlbridge-supernorte  ? supernorte.tpqsolutions.com.ar
```

**Mantén consistencia** entre el nombre del tunnel y el subdominio para facilitar la administración.

---

**? ¡Todo listo para crear subdominios!**

Ahora puedes crear tantos subdominios como clientes tengas, sin límites y sin costos adicionales.
