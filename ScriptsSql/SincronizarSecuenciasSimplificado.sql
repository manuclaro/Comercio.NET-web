-- ================================================================================
-- Script Simple para Sincronizar Secuencias de Tablas Principales
-- ================================================================================
-- 
-- Este script sincroniza las secuencias de las tablas más comunes del sistema
-- ================================================================================

-- Tabla: ventas
SELECT setval('ventas_id_seq', COALESCE((SELECT MAX(id) FROM ventas), 1), true);

-- Tabla: facturas
SELECT setval('facturas_idfactura_seq', COALESCE((SELECT MAX(idfactura) FROM facturas), 1), true);

-- Tabla: productos (si usa secuencia)
-- SELECT setval('productos_id_seq', COALESCE((SELECT MAX(id) FROM productos), 1), true);

-- Tabla: clientes (si usa secuencia)
-- SELECT setval('clientes_id_seq', COALESCE((SELECT MAX(id) FROM clientes), 1), true);

-- Tabla: proveedores (si usa secuencia)
-- SELECT setval('proveedores_id_seq', COALESCE((SELECT MAX(id) FROM proveedores), 1), true);

-- Tabla: usuarios (si usa secuencia)
-- SELECT setval('usuarios_id_seq', COALESCE((SELECT MAX(id) FROM usuarios), 1), true);

-- Tabla: cuentascorrientes (si usa secuencia)
-- SELECT setval('cuentascorrientes_id_seq', COALESCE((SELECT MAX(id) FROM cuentascorrientes), 1), true);

-- Tabla: movimientoscaja (si usa secuencia)
-- SELECT setval('movimientoscaja_id_seq', COALESCE((SELECT MAX(id) FROM movimientoscaja), 1), true);

-- Tabla: auditoriaeliminaciones (si usa secuencia)
-- SELECT setval('auditoriaeliminaciones_id_seq', COALESCE((SELECT MAX(id) FROM auditoriaeliminaciones), 1), true);

-- Tabla: retirosefectivo (si usa secuencia)
-- SELECT setval('retirosefectivo_id_seq', COALESCE((SELECT MAX(id) FROM retirosefectivo), 1), true);

-- ================================================================================
-- NOTA: 
-- Descomente las líneas de las tablas que use su sistema.
-- El nombre de las secuencias sigue el patrón: tabla_columna_seq
-- ================================================================================

-- Verificación final
SELECT 'ventas' as tabla, 
       currval('ventas_id_seq') AS secuencia, 
       (SELECT MAX(id) FROM ventas) AS max_id
UNION ALL
SELECT 'facturas' as tabla, 
       currval('facturas_idfactura_seq') AS secuencia, 
       (SELECT MAX(idfactura) FROM facturas) AS max_id;

-- Agregar más UNION ALL según las tablas que descomente arriba
