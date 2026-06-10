-- SCRIPT DE CORRECCIÓN: Establecer valores por defecto para EditarPrecio y PermiteAcumular
-- Motor: PostgreSQL
-- Descripción: Establece valores por defecto razonables para los productos que tienen NULL o false incorrectamente.
--
-- INSTRUCCIONES:
--   1. Conectarse a la base de datos con psql o DBeaver/pgAdmin.
--   2. Revisar el estado actual de los productos antes de ejecutar.
--   3. Ejecutar este script para corregir los valores.
-- =============================================================================

-- Verificar el estado actual (ejecutar antes y después)
SELECT 
    COUNT(*) FILTER (WHERE "EditarPrecio" = true) AS productos_con_editar_precio,
    COUNT(*) FILTER (WHERE "EditarPrecio" = false OR "EditarPrecio" IS NULL) AS productos_sin_editar_precio,
    COUNT(*) FILTER (WHERE "PermiteAcumular" = true) AS productos_con_acumular,
    COUNT(*) FILTER (WHERE "PermiteAcumular" = false OR "PermiteAcumular" IS NULL) AS productos_sin_acumular,
    COUNT(*) AS total_productos
FROM productos;

-- ESTRATEGIA DE CORRECCIÓN:
-- 1. Por defecto, la mayoría de productos NO deben permitir editar precio (false)
-- 2. Por defecto, la mayoría de productos SÍ deben permitir acumular (true)
-- 3. Solo productos especiales (ej: carnaza, productos a granel) deben tener EditarPrecio=true y PermiteAcumular=false

-- Actualizar productos: establecer PermiteAcumular=true por defecto
UPDATE productos
SET "PermiteAcumular" = true
WHERE "PermiteAcumular" IS NULL OR "PermiteAcumular" = false;

-- Actualizar columnas legacy bit si existen
UPDATE productos
SET permiteacumular = B'1'
WHERE permiteacumular IS NULL OR permiteacumular = B'0';

-- Actualizar productos: establecer EditarPrecio=false por defecto
-- (solo algunos productos especiales deben tener EditarPrecio=true, configúrelos manualmente después)
UPDATE productos
SET "EditarPrecio" = false
WHERE "EditarPrecio" IS NULL;

-- Actualizar columnas legacy bit si existen
UPDATE productos
SET editarprecio = B'0'
WHERE editarprecio IS NULL;

-- Verificar el resultado
SELECT 
    COUNT(*) FILTER (WHERE "EditarPrecio" = true) AS productos_con_editar_precio,
    COUNT(*) FILTER (WHERE "EditarPrecio" = false OR "EditarPrecio" IS NULL) AS productos_sin_editar_precio,
    COUNT(*) FILTER (WHERE "PermiteAcumular" = true) AS productos_con_acumular,
    COUNT(*) FILTER (WHERE "PermiteAcumular" = false OR "PermiteAcumular" IS NULL) AS productos_sin_acumular,
    COUNT(*) AS total_productos
FROM productos;

-- Listar productos que tienen EditarPrecio=true (debería ser una lista corta)
SELECT codigo, descripcion, "EditarPrecio", "PermiteAcumular"
FROM productos
WHERE "EditarPrecio" = true
ORDER BY descripcion;

COMMENT ON COLUMN productos."EditarPrecio" IS 'true = permite ingresar precio manualmente en cada venta (ej: carnaza, productos a granel)';
COMMENT ON COLUMN productos."PermiteAcumular" IS 'true = acumula cantidad en la factura; false = siempre nueva línea (usar con EditarPrecio=true para precios manuales)';
