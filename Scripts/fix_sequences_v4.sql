-- ============================================================
-- Script v4: Deteccion automatica de columna PK por convencion
-- Busca en cada tabla la primera columna de tipo entero cuyo
-- nombre sea "id" o empiece con "id" (idfactura, idproducto...)
-- Agrega PRIMARY KEY y secuencia si no los tiene.
-- Ejecutar UNA SOLA VEZ en la base de datos.
-- ============================================================


DO $$
DECLARE
    rec         RECORD;
    seq_name    TEXT;
    max_id      BIGINT;
    tiene_dupes BOOLEAN;
BEGIN
    -- Recorrer todas las tablas del schema public
    FOR rec IN
        SELECT
            t.table_name,
            -- Tomar la primera columna que sea tipo entero y nombre empiece con "id"
            (
                SELECT c.column_name
                FROM information_schema.columns c
                WHERE c.table_schema = 'public'
                  AND c.table_name   = t.table_name
                  AND c.data_type    IN ('integer', 'bigint', 'smallint', 'numeric')
                  AND (c.column_name = 'id' OR c.column_name LIKE 'id%')
                ORDER BY c.ordinal_position ASC
                LIMIT 1
            ) AS pk_col,
            -- Ver si ya tiene PRIMARY KEY
            EXISTS (
                SELECT 1 FROM information_schema.table_constraints tc
                WHERE tc.table_schema    = 'public'
                  AND tc.table_name     = t.table_name
                  AND tc.constraint_type = 'PRIMARY KEY'
            ) AS tiene_pk
        FROM information_schema.tables t
        WHERE t.table_schema = 'public'
          AND t.table_type   = 'BASE TABLE'
        ORDER BY t.table_name
    LOOP
        -- Si no se encontro columna candidata, omitir
        IF rec.pk_col IS NULL THEN
            RAISE NOTICE '[SKIP] %: no tiene columna tipo id*', rec.table_name;
            CONTINUE;
        END IF;

        -- Si ya tiene PRIMARY KEY, solo asegurar que tenga secuencia
        IF rec.tiene_pk THEN
            -- Verificar si le falta DEFAULT (secuencia)
            IF EXISTS (
                SELECT 1 FROM information_schema.columns c
                WHERE c.table_schema   = 'public'
                  AND c.table_name     = rec.table_name
                  AND c.column_name    = rec.pk_col
                  AND c.column_default IS NULL
            ) THEN
                seq_name := rec.table_name || '_' || rec.pk_col || '_seq';
                -- Si es numeric, convertir a bigint primero
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name   = rec.table_name
                      AND column_name  = rec.pk_col
                      AND data_type    = 'numeric'
                ) THEN
                    EXECUTE format(
                        'ALTER TABLE public.%I ALTER COLUMN %I TYPE bigint USING %I::bigint',
                        rec.table_name, rec.pk_col, rec.pk_col
                    );
                    RAISE NOTICE '[CAST] %.%: numeric -> bigint', rec.table_name, rec.pk_col;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = seq_name) THEN
                    EXECUTE format('CREATE SEQUENCE public.%I', seq_name);
                END IF;
                EXECUTE format('SELECT COALESCE(MAX(%I), 0) FROM public.%I', rec.pk_col, rec.table_name) INTO max_id;
                EXECUTE format('SELECT setval(''public.%I'', %s)', seq_name, GREATEST(max_id, 1));
                EXECUTE format('ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT nextval(''public.%I'')', rec.table_name, rec.pk_col, seq_name);
                EXECUTE format('ALTER SEQUENCE public.%I OWNED BY public.%I.%I', seq_name, rec.table_name, rec.pk_col);
                RAISE NOTICE '[SEQ] %: secuencia creada para columna % (max=%)', rec.table_name, rec.pk_col, max_id;
            ELSE
                RAISE NOTICE '[OK]  %: PRIMARY KEY en % ya tiene secuencia', rec.table_name, rec.pk_col;
            END IF;
            CONTINUE;
        END IF;

        -- Verificar que no haya duplicados en la columna candidata (requisito para PK)
        EXECUTE format(
            'SELECT EXISTS (SELECT 1 FROM public.%I GROUP BY %I HAVING COUNT(*) > 1 LIMIT 1)',
            rec.table_name, rec.pk_col
        ) INTO tiene_dupes;

        IF tiene_dupes THEN
            RAISE NOTICE '[WARN] %: columna % tiene valores duplicados, no se puede hacer PK', rec.table_name, rec.pk_col;
            CONTINUE;
        END IF;

        -- Si la columna es numeric, convertirla a bigint para poder usar secuencia
        IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name   = rec.table_name
              AND column_name  = rec.pk_col
              AND data_type    = 'numeric'
        ) THEN
            EXECUTE format(
                'ALTER TABLE public.%I ALTER COLUMN %I TYPE bigint USING %I::bigint',
                rec.table_name, rec.pk_col, rec.pk_col
            );
            RAISE NOTICE '[CAST] %.%: numeric -> bigint', rec.table_name, rec.pk_col;
        END IF;

        -- Crear secuencia si no existe
        seq_name := rec.table_name || '_' || rec.pk_col || '_seq';

        IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = seq_name) THEN
            EXECUTE format('CREATE SEQUENCE public.%I', seq_name);
        END IF;

        -- Sincronizar secuencia con el valor maximo actual
        EXECUTE format('SELECT COALESCE(MAX(%I), 0) FROM public.%I', rec.pk_col, rec.table_name) INTO max_id;
        EXECUTE format('SELECT setval(''public.%I'', %s)', seq_name, GREATEST(max_id, 1));

        -- Asignar DEFAULT a la columna
        EXECUTE format(
            'ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT nextval(''public.%I'')',
            rec.table_name, rec.pk_col, seq_name
        );

        -- Marcar como propietaria
        EXECUTE format(
            'ALTER SEQUENCE public.%I OWNED BY public.%I.%I',
            seq_name, rec.table_name, rec.pk_col
        );

        -- Agregar la PRIMARY KEY
        EXECUTE format(
            'ALTER TABLE public.%I ADD PRIMARY KEY (%I)',
            rec.table_name, rec.pk_col
        );

        RAISE NOTICE '[NEW] %: PRIMARY KEY agregada en % + secuencia creada (max=%)',
            rec.table_name, rec.pk_col, max_id;

    END LOOP;

    RAISE NOTICE '=== Script v4 completado ===';
END;
$$;

-- ============================================================
-- Verificacion final
-- ============================================================
SELECT
    t.table_name,
    COALESCE(k.column_name, '*** SIN PRIMARY KEY ***') AS columna_pk,
    COALESCE(c.column_default, '*** SIN SECUENCIA ***') AS secuencia
FROM information_schema.tables t
LEFT JOIN information_schema.table_constraints tc
    ON  tc.table_name      = t.table_name
    AND tc.table_schema    = t.table_schema
    AND tc.constraint_type = 'PRIMARY KEY'
LEFT JOIN information_schema.key_column_usage k
    ON  k.constraint_name = tc.constraint_name
    AND k.table_schema    = tc.table_schema
LEFT JOIN information_schema.columns c
    ON  c.table_name   = k.table_name
    AND c.column_name  = k.column_name
    AND c.table_schema = 'public'
WHERE t.table_schema = 'public'
  AND t.table_type   = 'BASE TABLE'
ORDER BY t.table_name;
