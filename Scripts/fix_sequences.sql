-- ============================================================
-- Script para reparar columnas ID sin secuencia en PostgreSQL
-- Migracion desde SQL Server
-- ============================================================
-- Ejecutar este script UNA SOLA VEZ en la base de datos.
-- Para cada tabla: crea la secuencia si no existe,
-- la sincroniza con el valor maximo actual del ID,
-- y asigna el DEFAULT a la columna.
-- ============================================================

DO $$
DECLARE
    tablas TEXT[] := ARRAY[
        'ventas',
        'facturas',
        'productos',
        'usuarios',
        'turnoscajero',
        'cierreturnoscajero',
        'ofertasproductos',
        'detalleofertasproductos',
        'proveedores',
        'comprasproveedores',
        'comprasproveedoresctacte',
        'comprasproveedoresivadetalle',
        'comprasproveedorespagos',
        'ctacteproveedores',
        'pagosproveedores',
        'retirosefectivo',
        'detallespagofactura',
        'auditoriaproductos',
        'auditoriaproductoseliminados',
        'permisosperfiles',
        'formaspago',
        'mesas',
        'mesasitems',
        'mozos',
        'productosbar'
    ];
    tabla TEXT;
    seq_name TEXT;
    max_id BIGINT;
    col_exists BOOLEAN;
BEGIN
    FOREACH tabla IN ARRAY tablas
    LOOP
        -- Verificar si la tabla existe
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name = tabla
        ) THEN
            RAISE NOTICE 'Tabla % no existe, omitiendo...', tabla;
            CONTINUE;
        END IF;

        -- Verificar si la columna "id" existe en la tabla
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name   = tabla
              AND column_name  = 'id'
        ) INTO col_exists;

        IF NOT col_exists THEN
            RAISE NOTICE 'Tabla % no tiene columna id, omitiendo...', tabla;
            CONTINUE;
        END IF;

        -- Verificar si ya tiene DEFAULT (secuencia asignada)
        IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema   = 'public'
              AND table_name     = tabla
              AND column_name    = 'id'
              AND column_default IS NOT NULL
        ) THEN
            RAISE NOTICE 'Tabla % ya tiene DEFAULT en id, omitiendo...', tabla;
            CONTINUE;
        END IF;

        seq_name := tabla || '_id_seq';

        -- Crear la secuencia si no existe
        IF NOT EXISTS (
            SELECT 1 FROM pg_sequences
            WHERE schemaname = 'public' AND sequencename = seq_name
        ) THEN
            EXECUTE format('CREATE SEQUENCE public.%I', seq_name);
            RAISE NOTICE 'Secuencia % creada.', seq_name;
        END IF;

        -- Sincronizar la secuencia con el valor maximo actual
        EXECUTE format('SELECT COALESCE(MAX(id), 0) FROM public.%I', tabla) INTO max_id;
        EXECUTE format('SELECT setval(''public.%I'', %s)', seq_name, GREATEST(max_id, 1));

        -- Asignar el DEFAULT a la columna id
        EXECUTE format(
            'ALTER TABLE public.%I ALTER COLUMN id SET DEFAULT nextval(''public.%I'')',
            tabla, seq_name
        );

        -- Asignar la tabla como propietaria de la secuencia
        EXECUTE format(
            'ALTER SEQUENCE public.%I OWNED BY public.%I.id',
            seq_name, tabla
        );

        RAISE NOTICE 'Tabla %: secuencia % configurada (max_id=%).', tabla, seq_name, max_id;
    END LOOP;
END;
$$;
