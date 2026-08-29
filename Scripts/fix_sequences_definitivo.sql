-- =============================================================================
-- fix_sequences_definitivo.sql
-- =============================================================================
-- Sincroniza TODAS las secuencias de la base de datos con el MAX(id) real
-- de cada tabla. Usa pg_depend para obtener la relación exacta entre
-- secuencia ? tabla ? columna, sin depender del nombre de la secuencia.
--
-- Ejecutar en psql o pgAdmin después de una migración de datos.
-- Es seguro ejecutarlo múltiples veces.
-- =============================================================================

DO $$
DECLARE
    r           RECORD;
    max_val     BIGINT;
    new_val     BIGINT;
BEGIN
    RAISE NOTICE '=============================================================';
    RAISE NOTICE 'Iniciando sincronización de secuencias...';
    RAISE NOTICE '=============================================================';

    -- pg_depend con deptype='a' contiene la relación real secuencia ? columna propietaria
    FOR r IN
        SELECT
            seq.relname                          AS seq_name,
            nsp.nspname                          AS schema_name,
            tbl.relname                          AS table_name,
            att.attname                          AS col_name
        FROM pg_class       seq
        JOIN pg_depend      dep ON dep.objid        = seq.oid
                                AND dep.deptype     = 'a'
                                AND dep.classid     = 'pg_class'::regclass
        JOIN pg_class       tbl ON tbl.oid          = dep.refobjid
        JOIN pg_attribute   att ON att.attrelid     = dep.refobjid
                                AND att.attnum      = dep.refobjsubid
        JOIN pg_namespace   nsp ON nsp.oid          = tbl.relnamespace
        WHERE seq.relkind   = 'S'          -- solo secuencias
          AND nsp.nspname   = 'public'     -- solo schema public
        ORDER BY tbl.relname, att.attname
    LOOP
        BEGIN
            -- Obtener el MAX actual de la columna
            EXECUTE format(
                'SELECT COALESCE(MAX(%I), 0) FROM %I.%I',
                r.col_name, r.schema_name, r.table_name
            ) INTO max_val;

            -- El próximo valor debe ser MAX + 1
            new_val := GREATEST(max_val + 1, 1);

            -- Sincronizar la secuencia (false = el próximo nextval() devolverá new_val)
            EXECUTE format(
                'SELECT setval(%L, %s, false)',
                r.schema_name || '.' || r.seq_name,
                new_val
            );

            RAISE NOTICE '[OK] %.% ? secuencia "%" ? MAX actual: % ? próximo valor: %',
                r.table_name, r.col_name, r.seq_name, max_val, new_val;

        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE '[ERROR] %.% ? "%": %',
                r.table_name, r.col_name, r.seq_name, SQLERRM;
        END;
    END LOOP;

    RAISE NOTICE '=============================================================';
    RAISE NOTICE 'Sincronización completada.';
    RAISE NOTICE '=============================================================';
END $$;


-- =============================================================================
-- Verificación: muestra MAX(id) real vs. próximo valor de secuencia por tabla.
-- Si "max_id_tabla" > "proximo_nextval - 1" ? la secuencia sigue desincronizada.
-- =============================================================================
SELECT
    tbl.relname                                                         AS tabla,
    att.attname                                                         AS columna,
    seq.relname                                                         AS secuencia,
    -- Próximo valor que generará nextval()
    CASE
        WHEN ps.last_value IS NULL THEN 1
        WHEN ps.is_called   THEN ps.last_value + ps.increment_by
        ELSE ps.last_value
    END                                                                 AS proximo_nextval
FROM pg_class       seq
JOIN pg_depend      dep ON dep.objid       = seq.oid
                        AND dep.deptype    = 'a'
                        AND dep.classid    = 'pg_class'::regclass
JOIN pg_class       tbl ON tbl.oid         = dep.refobjid
JOIN pg_attribute   att ON att.attrelid    = dep.refobjid
                        AND att.attnum     = dep.refobjsubid
JOIN pg_namespace   nsp ON nsp.oid         = tbl.relnamespace
JOIN pg_sequences   ps  ON ps.schemaname   = nsp.nspname
                        AND ps.sequencename = seq.relname
WHERE seq.relkind   = 'S'
  AND nsp.nspname   = 'public'
ORDER BY tbl.relname;
