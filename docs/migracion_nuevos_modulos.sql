-- =============================================================================
-- SCRIPT DE MIGRACIÓN: Comercio.NET Web - Nuevos Módulos
-- Motor: PostgreSQL
-- Descripción: Crea/amplía las tablas necesarias para los módulos de
--   Administrador de Productos, Proveedores, Usuarios y Cuentas Corrientes.
--
-- INSTRUCCIONES:
--   1. Conectarse a la base de datos con psql o DBeaver/pgAdmin.
--   2. Ejecutar este script completo.
--   3. Cada bloque usa IF NOT EXISTS o ALTER TABLE ... ADD COLUMN IF NOT EXISTS
--      por lo que es seguro ejecutarlo más de una vez.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- 0. EXTENSIÓN (por si la DB no la tiene habilitada)
-- ---------------------------------------------------------------------------
-- CREATE EXTENSION IF NOT EXISTS "unaccent";


-- ===========================================================================
-- 1. TABLA: productos
--    La tabla ya existe. Se agregan las columnas nuevas si no están presentes.
-- ===========================================================================

ALTER TABLE productos
    ADD COLUMN IF NOT EXISTS porcentajeiva   NUMERIC(5,2)  NOT NULL DEFAULT 21,
    ADD COLUMN IF NOT EXISTS codigobarras    VARCHAR(50)   NULL;

-- Aseguramos que la columna "activo" exista (en algunos esquemas puede no estar)
-- Ya existe en tu sistema, pero la incluimos de forma segura:
-- ALTER TABLE productos ADD COLUMN IF NOT EXISTS activo BIT NOT NULL DEFAULT B'1';

COMMENT ON COLUMN productos.porcentajeiva IS 'Alícuota de IVA aplicable (0, 10.5, 21, 27)';
COMMENT ON COLUMN productos.codigobarras  IS 'Código de barras EAN-13 / código interno adicional';


-- ===========================================================================
-- 2. TABLA: proveedores
--    La tabla ya existe con: id, nombre, cuit, domicilio, telefono, activo
--    Se agregan las columnas de email y contacto si no están presentes.
-- ===========================================================================

ALTER TABLE proveedores
    ADD COLUMN IF NOT EXISTS email     VARCHAR(200) NULL,
    ADD COLUMN IF NOT EXISTS contacto  VARCHAR(200) NULL;

COMMENT ON COLUMN proveedores.email    IS 'Email de contacto del proveedor';
COMMENT ON COLUMN proveedores.contacto IS 'Nombre del contacto en el proveedor';


-- ===========================================================================
-- 3. TABLA: ctacte  (clientes de cuenta corriente)
--    Puede que ya exista (los nombres se guardan en appsettings.json),
--    pero creamos la tabla normalizada si no existe.
-- ===========================================================================

CREATE TABLE IF NOT EXISTS ctacte (
    id          SERIAL       PRIMARY KEY,
    nombre      VARCHAR(200) NOT NULL,
    dni         VARCHAR(20)  NULL,
    telefono    VARCHAR(50)  NULL,
    email       VARCHAR(200) NULL,
    activo      BIT          NOT NULL DEFAULT B'1',
    fechaalta   TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- Índice para búsqueda rápida por nombre
CREATE INDEX IF NOT EXISTS idx_ctacte_nombre  ON ctacte (LOWER(nombre));
CREATE INDEX IF NOT EXISTS idx_ctacte_activo  ON ctacte (activo);

COMMENT ON TABLE  ctacte           IS 'Clientes habilitados para comprar en cuenta corriente';
COMMENT ON COLUMN ctacte.activo    IS 'B''1'' = activo, B''0'' = dado de baja';


-- ===========================================================================
-- 4. TABLA: pagosctacte  (pagos que realizan los clientes de ctacte)
--    Esta es la tabla NUEVA que necesita el módulo web.
-- ===========================================================================

CREATE TABLE IF NOT EXISTS pagosctacte (
    id           SERIAL          PRIMARY KEY,
    cliente_id   INT             NOT NULL REFERENCES ctacte(id) ON DELETE RESTRICT,
    monto        NUMERIC(18,2)   NOT NULL CHECK (monto > 0),
    mediopago    VARCHAR(50)     NOT NULL DEFAULT 'Efectivo',
    referencia   VARCHAR(500)    NULL,
    usuario      VARCHAR(100)    NULL,
    fecha        TIMESTAMP       NOT NULL DEFAULT NOW(),
    fecharegistro TIMESTAMP      NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pagosctacte_cliente ON pagosctacte (cliente_id);
CREATE INDEX IF NOT EXISTS idx_pagosctacte_fecha   ON pagosctacte (fecha);

COMMENT ON TABLE  pagosctacte             IS 'Pagos realizados por clientes de cuenta corriente';
COMMENT ON COLUMN pagosctacte.monto       IS 'Monto abonado (siempre positivo)';
COMMENT ON COLUMN pagosctacte.mediopago   IS 'Efectivo, Transferencia, Débito, Crédito, Cheque, Otro';
COMMENT ON COLUMN pagosctacte.referencia  IS 'Número de transferencia, cheque, etc.';


-- ===========================================================================
-- 5. TABLA: usuarios
--    Ya existe. Verificamos que tengan las columnas que usa la app web.
--    Las columnas adicionales de permisos ya están en tu esquema.
-- ===========================================================================

-- En tu sistema la tabla se llama "usuarios" (minúsculas).
-- Las columnas que usa la API web: idusuarios, nombreusuario, nombre, apellido,
-- nivel, numerocajero, passwordhash, activo
-- Todas ya existen. Solo nos aseguramos del tipo de activo.

-- Si activo fuera BOOLEAN (como muestra AuthenticationService.cs) y la API usa BIT,
-- dejar como está: el query de la API ya adapta ambos tipos con "activo::text IN ('true','1','t')"


-- ===========================================================================
-- 6. MIGRAR nombres existentes de ctacte (desde appsettings) — OPCIONAL
--    Si ya tenés nombres guardados en appsettings.json bajo
--    "CuentasCorrientes:NombresCtaCte", podés insertarlos acá.
--    Descomentá y reemplazá con los nombres reales.
-- ===========================================================================

/*
INSERT INTO ctacte (nombre) VALUES
    ('Juan Pérez'),
    ('María García'),
    ('Carlos López')
ON CONFLICT DO NOTHING;
*/


-- ===========================================================================
-- 7. VERIFICACIÓN FINAL
-- ===========================================================================

SELECT 'productos'     AS tabla, COUNT(*) AS filas FROM productos
UNION ALL
SELECT 'proveedores',           COUNT(*)            FROM proveedores
UNION ALL
SELECT 'ctacte',                COUNT(*)            FROM ctacte
UNION ALL
SELECT 'pagosctacte',           COUNT(*)            FROM pagosctacte
UNION ALL
SELECT 'usuarios',              COUNT(*)            FROM usuarios;

-- Mostrar columnas nuevas de productos
SELECT column_name, data_type, is_nullable
FROM   information_schema.columns
WHERE  table_name = 'productos'
  AND  column_name IN ('porcentajeiva','codigobarras','activo')
ORDER BY column_name;

-- Mostrar columnas nuevas de proveedores
SELECT column_name, data_type, is_nullable
FROM   information_schema.columns
WHERE  table_name = 'proveedores'
  AND  column_name IN ('email','contacto','domicilio')
ORDER BY column_name;
