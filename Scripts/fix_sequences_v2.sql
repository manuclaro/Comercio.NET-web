-- ============================================================
-- Script v2: Reparar TODAS las PKs sin secuencia automaticamente
-- Detecta cada tabla y su columna PK real (sin importar el nombre)
-- Migracion desde SQL Server a PostgreSQL
-- Ejecutar UNA SOLA VEZ en la base de datos.
-- ============================================================

DO $$
DECLARE
    rec         RECORD;
    seq_name    TEXT;
    max_id      BIGINT;
    col_type    TEXT;
BEGIN
    -- Recorrer todas las columnas PK de tipo entero que NO tienen DEFAULT
    FOR rec IN
        SELECT
            t.table_name,
            c.column_name,
            c.data_type,
            c.column_default
        FROM information_schema.table_constraints t
        JOIN information_schema.key_column_usage  k
            ON  k.constraint_name = t.constraint_name
            AND k.table_schema    = t.table_schema
        JOIN information_schema.columns c
            ON  c.table_name   = k.table_name
            AND c.column_name  = k.column_name
            AND c.table_schema = t.table_schema
        WHERE t.constraint_type = 'PRIMARY KEY'
          AND t.table_schema    = 'public'
          AND c.data_type       IN ('integer', 'bigint', 'smallint', 'numeric')
          AND c.column_default  IS NULL
        ORDER BY t.table_name
    LOOP
        seq_name := rec.table_name || '_' || rec.column_name || '_seq';

        -- Crear la secuencia si no existe
        IF NOT EXISTS (
            SELECT 1 FROM pg_sequences
            WHERE schemaname = 'public' AND sequencename = seq_name
        ) THEN
            EXECUTE format('CREATE SEQUENCE public.%I', seq_name);
            RAISE NOTICE '[NUEVA] Secuencia % creada para %.%',
                seq_name, rec.table_name, rec.column_name;
        END IF;

        -- Sincronizar con el valor maximo actual
        EXECUTE format(
            'SELECT COALESCE(MAX(%I), 0) FROM public.%I',
            rec.column_name, rec.table_name
        ) INTO max_id;

        EXECUTE format(
            'SELECT setval(''public.%I'', %s)',
            seq_name, GREATEST(max_id, 1)
        );

        -- Asignar el DEFAULT a la columna PK
        EXECUTE format(
            'ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT nextval(''public.%I'')',
            rec.table_name, rec.column_name, seq_name
        );

        -- Marcar la secuencia como propiedad de la columna
        EXECUTE format(
            'ALTER SEQUENCE public.%I OWNED BY public.%I.%I',
            seq_name, rec.table_name, rec.column_name
        );

        RAISE NOTICE '[OK] %.% -> secuencia % (max=%)',
            rec.table_name, rec.column_name, seq_name, max_id;
    END LOOP;

    RAISE NOTICE '=== Proceso completado ===';
END;
$$;

-- Verificacion: mostrar todas las PKs de la base con su DEFAULT actual
SELECT
    t.table_name,
    k.column_name,
    c.data_type,
    CASE
        WHEN c.column_default IS NOT NULL THEN 'OK - ' || c.column_default
        ELSE '*** SIN DEFAULT ***'
    END AS estado
FROM information_schema.table_constraints t
JOIN information_schema.key_column_usage  k
    ON  k.constraint_name = t.constraint_name
    AND k.table_schema    = t.table_schema
JOIN information_schema.columns c
    ON  c.table_name   = k.table_name
    AND c.column_name  = k.column_name
    AND c.table_schema = t.table_schema
WHERE t.constraint_type = 'PRIMARY KEY'
  AND t.table_schema    = 'public'
  AND c.data_type       IN ('integer', 'bigint', 'smallint', 'numeric')
ORDER BY t.table_name;
