-- =============================================================================
-- diagnostico_ventas.sql
-- =============================================================================
-- Muestra el MAX(id) real de la tabla ventas vs. el próximo valor de la secuencia.
-- Si max_id_tabla >= proximo_nextval ? la secuencia está desincronizada todavía.
-- =============================================================================

-- 1) Ver el MAX real en la tabla y el estado de la secuencia
SELECT
    (SELECT MAX(id) FROM ventas)                            AS max_id_tabla,
    last_value                                              AS last_value_secuencia,
    is_called                                               AS is_called,
    CASE
        WHEN is_called THEN last_value + increment_by
        ELSE last_value
    END                                                     AS proximo_nextval
FROM pg_sequences
WHERE schemaname = 'public'
  AND sequencename = 'ventas_id_seq';

-- Si max_id_tabla >= proximo_nextval, correr esto:
-- SELECT setval('public.ventas_id_seq', (SELECT MAX(id) FROM ventas) + 1, false);

-- 2) Corregir la secuencia de ventas directamente (ejecutar siempre es seguro)
SELECT setval(
    'public.ventas_id_seq',
    COALESCE((SELECT MAX(id) FROM ventas), 0) + 1,
    false   -- false = el próximo nextval() devolverá exactamente este valor
);

-- 3) Verificar resultado
SELECT
    (SELECT MAX(id) FROM ventas)                            AS max_id_tabla,
    last_value                                              AS nuevo_last_value,
    CASE
        WHEN is_called THEN last_value + increment_by
        ELSE last_value
    END                                                     AS proximo_nextval_ahora
FROM pg_sequences
WHERE schemaname = 'public'
  AND sequencename = 'ventas_id_seq';
