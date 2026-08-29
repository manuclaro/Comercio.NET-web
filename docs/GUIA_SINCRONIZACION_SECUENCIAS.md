# Sincronización de Secuencias - Guía Completa

## ?? Archivos Creados

### 1. `SincronizarTodasLasSecuencias.sql` (Recomendado)
**Descripción:** Script dinámico que encuentra y sincroniza TODAS las secuencias automáticamente.

**Ventajas:**
- ? Encuentra automáticamente todas las secuencias
- ? No requiere mantenimiento cuando se agregan nuevas tablas
- ? Muestra mensajes detallados de progreso
- ? Maneja errores gracefully

**Cuándo usar:**
- Para sincronizar toda la base de datos de una vez
- Después de importar datos o restaurar backups
- Como mantenimiento preventivo periódico

### 2. `SincronizarSecuenciasSimplificado.sql`
**Descripción:** Script simple y directo para las tablas principales.

**Ventajas:**
- ? Más fácil de entender y modificar
- ? Control exacto sobre qué tablas sincronizar
- ? Ejecución más rápida

**Cuándo usar:**
- Cuando solo necesitas sincronizar tablas específicas
- Para soluciones rápidas y puntuales
- Si prefieres tener control manual

### 3. `SincronizarSecuenciaVentas.sql` (Ya existente)
**Descripción:** Script específico para la tabla ventas.

**Cuándo usar:**
- Solo para problemas en la tabla ventas
- Ejecución muy rápida y específica

## ?? Cómo Ejecutar

### Opción A: Script Completo Automático (Recomendado)

1. Abrir **pgAdmin** o cualquier cliente PostgreSQL
2. Conectarse a la base de datos
3. Abrir el script `SincronizarTodasLasSecuencias.sql`
4. Ejecutar (F5 o botón Execute)
5. Ver los mensajes de confirmación en la ventana de mensajes

### Opción B: Script Simplificado

1. Abrir `SincronizarSecuenciasSimplificado.sql`
2. **Descomentar** las líneas de las tablas que uses
3. Ejecutar el script
4. Verificar los resultados al final

### Opción C: Scripts Individuales

Ejecutar cada script individual según la tabla que necesites:
- `SincronizarSecuenciaVentas.sql` ? Solo ventas
- O crear scripts similares para otras tablas

## ?? Verificación

Para verificar que todas las secuencias están correctas, ejecuta:

```sql
SELECT 
    schemaname,
    sequencename,
    last_value,
    is_called
FROM 
    pg_sequences
WHERE 
    schemaname = 'public'
ORDER BY 
    sequencename;
```

## ??? Prevención Automática

El formulario de **Ventas** ya sincroniza automáticamente su secuencia al abrirse.

Si quieres agregar sincronización automática a otros formularios, avísame y puedo implementarlo.

## ?? Cuándo Ejecutar

### Ejecutar SIEMPRE después de:
- ? Importar datos desde Excel/CSV
- ? Restaurar un backup de la base de datos
- ? Migrar datos desde otro sistema
- ? Insertar registros con IDs explícitos

### Ejecutar como MANTENIMIENTO:
- ?? Semanalmente (preventivo)
- ?? Después de actualizaciones importantes
- ?? Si aparecen errores de "llave duplicada"

## ?? Recomendación

**Para uso general:** Ejecuta `SincronizarTodasLasSecuencias.sql` una vez ahora y luego establece una rutina de ejecutarlo mensualmente como mantenimiento preventivo.

**Para problemas específicos:** Usa el script simplificado o individual de la tabla afectada.

## ?? Tip Pro

Puedes crear un trabajo programado (cron job) en PostgreSQL para ejecutar el script automáticamente:

```sql
-- Ejemplo de función para ejecutar periódicamente
CREATE OR REPLACE FUNCTION sincronizar_secuencias_auto()
RETURNS void AS $$
BEGIN
    -- Código del script aquí
    -- (copiar el contenido del DO $$ del script completo)
END;
$$ LANGUAGE plpgsql;

-- Llamar manualmente cuando sea necesario:
-- SELECT sincronizar_secuencias_auto();
```

## ? ¿Necesitas Ayuda?

Si necesitas:
- Agregar sincronización automática a más formularios
- Crear una función en C# que sincronice todo
- Configurar ejecución automática periódica
- Personalizar los scripts para tu base de datos

¡Avísame y lo implemento!
