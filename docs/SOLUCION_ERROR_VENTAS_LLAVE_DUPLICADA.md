# Solución al Error: 23505 - Llave Duplicada en Tabla Ventas

## Problema
Al intentar agregar productos en el formulario de Ventas, aparece el siguiente error:

```
Error: 23505: llave duplicada viola restricción de unicidad «ventas_pkey»
DETAIL: Detail redacted as it may contain sensitive data. 
Specify 'Include Error Detail' in the connection string to include this information.
```

## Causa
Este error ocurre porque la **secuencia** de PostgreSQL que genera los IDs automáticos para la tabla `ventas` está **desincronizada** con el máximo ID actual de la tabla.

Esto puede suceder cuando:
- Se importan datos con IDs explícitos
- Se restaura un backup de la base de datos
- Se insertan registros manualmente con IDs específicos
- Hay operaciones de DELETE y re-INSERT que no actualizan la secuencia

## Solución Implementada

### Solución Automática
Se agregó código en el formulario de Ventas para sincronizar automáticamente la secuencia cada vez que se abre el formulario. Esto está en el método:

```csharp
private void SincronizarSecuenciaVentas(NpgsqlConnection connection)
```

Este método se llama en el evento `Ventas_Load` del formulario.

### Solución Manual (Emergencia)
Si necesita solucionar el problema inmediatamente sin reiniciar el formulario, puede ejecutar el siguiente script SQL directamente en PostgreSQL:

**Ubicación del script:** `ScriptsSql\SincronizarSecuenciaVentas.sql`

**Contenido del script:**
```sql
SELECT setval('ventas_id_seq', COALESCE((SELECT MAX(id) FROM ventas), 1), true);
```

### Cómo Ejecutar el Script Manual

1. **Abrir pgAdmin** o cualquier cliente de PostgreSQL
2. **Conectarse** a la base de datos del comercio
3. **Ejecutar el script** `SincronizarSecuenciaVentas.sql`
4. **Verificar** que la secuencia esté sincronizada

## Verificación
Para verificar que la secuencia está correctamente sincronizada, ejecute:

```sql
SELECT currval('ventas_id_seq') AS secuencia_actual, 
       (SELECT MAX(id) FROM ventas) AS max_id_tabla;
```

La `secuencia_actual` debe ser **igual o mayor** que `max_id_tabla`.

## Prevención
- La solución automática previene este problema en el futuro
- El formulario sincroniza la secuencia cada vez que se abre
- No es necesario realizar ninguna acción manual adicional

## Notas Técnicas
- **Archivo modificado:** `Formularios\Ventas.cs`
- **Método agregado:** `SincronizarSecuenciaVentas(NpgsqlConnection connection)`
- **Líneas modificadas:** ~4069-4116
- La sincronización se ejecuta de forma silenciosa y no interrumpe el flujo normal del programa
- Los errores de sincronización se registran en el log de debug pero no se muestran al usuario

## Fecha de Implementación
Enero 2025
