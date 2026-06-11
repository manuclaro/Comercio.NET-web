-- ================================================================================
-- Script para Sincronizar la Secuencia de la Tabla 'ventas'
-- ================================================================================
-- 
-- PROBLEMA:
-- Error: 23505: llave duplicada viola restricción de unicidad «ventas_pkey»
--
-- CAUSA:
-- La secuencia 'ventas_id_seq' está desincronizada con el máximo ID actual 
-- de la tabla 'ventas'. Esto ocurre cuando se importan datos con IDs explícitos
-- o después de restaurar un backup.
--
-- SOLUCIÓN:
-- Este script sincroniza la secuencia con el máximo ID actual de la tabla.
--
-- ================================================================================

-- Sincronizar la secuencia con el máximo ID de la tabla ventas
SELECT setval('ventas_id_seq', COALESCE((SELECT MAX(id) FROM ventas), 1), true);

-- Verificar la sincronización (opcional)
-- Este SELECT muestra el valor actual de la secuencia
SELECT currval('ventas_id_seq') AS secuencia_actual, 
       (SELECT MAX(id) FROM ventas) AS max_id_tabla;

-- ================================================================================
-- NOTA: 
-- Este script también se ejecuta automáticamente cada vez que se abre el 
-- formulario de Ventas, pero puede ejecutarlo manualmente si necesita 
-- solucionar el problema inmediatamente.
-- ================================================================================
