-- ============================================================
-- Script v3: Agregar PRIMARY KEY y secuencia a todas las tablas
-- que fueron migradas desde SQL Server sin sus constraints.
-- Ejecutar UNA SOLA VEZ en la base de datos.
-- ============================================================

DO $$
DECLARE
    rec RECORD;
BEGIN

    -- ============================================================
    -- PASO 1: Agregar PRIMARY KEY a las tablas que no la tienen
    -- pero tienen una columna id (o similar) como clave natural
    -- ============================================================

    -- Tabla: ventas (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'ventas' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.ventas ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] ventas.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] ventas ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: productos (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'productos' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.productos ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] productos.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] productos ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: facturas (PK: idfactura)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'facturas' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        -- Primero asignar secuencia a idfactura si no tiene
        IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'facturas_idfactura_seq') THEN
            CREATE SEQUENCE public.facturas_idfactura_seq;
        END IF;
        PERFORM setval('public.facturas_idfactura_seq', GREATEST((SELECT COALESCE(MAX(idfactura), 0) FROM public.facturas), 1));
        ALTER TABLE public.facturas ALTER COLUMN idfactura SET DEFAULT nextval('public.facturas_idfactura_seq');
        ALTER SEQUENCE public.facturas_idfactura_seq OWNED BY public.facturas.idfactura;
        ALTER TABLE public.facturas ADD PRIMARY KEY (idfactura);
        RAISE NOTICE '[PK] facturas.idfactura marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] facturas ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: usuarios (PK: depende del esquema - usar numerousuario o primer INT)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'usuarios' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        -- Verificar cual es la columna clave
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'usuarios' AND column_name = 'numerousuario' AND table_schema = 'public') THEN
            IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'usuarios_numerousuario_seq') THEN
                CREATE SEQUENCE public.usuarios_numerousuario_seq;
            END IF;
            PERFORM setval('public.usuarios_numerousuario_seq', GREATEST((SELECT COALESCE(MAX(numerousuario), 0) FROM public.usuarios), 1));
            ALTER TABLE public.usuarios ALTER COLUMN numerousuario SET DEFAULT nextval('public.usuarios_numerousuario_seq');
            ALTER SEQUENCE public.usuarios_numerousuario_seq OWNED BY public.usuarios.numerousuario;
            ALTER TABLE public.usuarios ADD PRIMARY KEY (numerousuario);
            RAISE NOTICE '[PK] usuarios.numerousuario marcada como PRIMARY KEY';
        ELSIF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'usuarios' AND column_name = 'id' AND table_schema = 'public') THEN
            IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'usuarios_id_seq') THEN
                CREATE SEQUENCE public.usuarios_id_seq;
            END IF;
            PERFORM setval('public.usuarios_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM public.usuarios), 1));
            ALTER TABLE public.usuarios ALTER COLUMN id SET DEFAULT nextval('public.usuarios_id_seq');
            ALTER SEQUENCE public.usuarios_id_seq OWNED BY public.usuarios.id;
            ALTER TABLE public.usuarios ADD PRIMARY KEY (id);
            RAISE NOTICE '[PK] usuarios.id marcada como PRIMARY KEY';
        ELSE
            RAISE NOTICE '[WARN] usuarios: no se encontro columna PK conocida';
        END IF;
    ELSE
        RAISE NOTICE '[SKIP] usuarios ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: ofertasproductos (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'ofertasproductos' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.ofertasproductos ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] ofertasproductos.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] ofertasproductos ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: detalleofertasproductos (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'detalleofertasproductos' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.detalleofertasproductos ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] detalleofertasproductos.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] detalleofertasproductos ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: proveedores (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'proveedores' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.proveedores ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] proveedores.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] proveedores ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: comprasproveedores (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'comprasproveedores' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.comprasproveedores ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] comprasproveedores.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] comprasproveedores ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: comprasproveedoresivadetalle (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'comprasproveedoresivadetalle' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.comprasproveedoresivadetalle ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] comprasproveedoresivadetalle.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] comprasproveedoresivadetalle ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: ctacteproveedores (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'ctacteproveedores' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.ctacteproveedores ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] ctacteproveedores.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] ctacteproveedores ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: pagosproveedores (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'pagosproveedores' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.pagosproveedores ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] pagosproveedores.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] pagosproveedores ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: retirosefectivo (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'retirosefectivo' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.retirosefectivo ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] retirosefectivo.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] retirosefectivo ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: detallespagofactura (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'detallespagofactura' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.detallespagofactura ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] detallespagofactura.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] detallespagofactura ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: auditoriaproductos (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'auditoriaproductos' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.auditoriaproductos ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] auditoriaproductos.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] auditoriaproductos ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: formaspago (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'formaspago' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.formaspago ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] formaspago.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] formaspago ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: mesas (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'mesas' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.mesas ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] mesas.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] mesas ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: mesasitems (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'mesasitems' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.mesasitems ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] mesasitems.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] mesasitems ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: mozos (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'mozos' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.mozos ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] mozos.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] mozos ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: productosbar (PK: id)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'productosbar' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        ALTER TABLE public.productosbar ADD PRIMARY KEY (id);
        RAISE NOTICE '[PK] productosbar.id marcada como PRIMARY KEY';
    ELSE
        RAISE NOTICE '[SKIP] productosbar ya tiene PRIMARY KEY';
    END IF;

    -- Tabla: administradores (PK: id si existe)
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                   WHERE table_name = 'administradores' AND constraint_type = 'PRIMARY KEY' AND table_schema = 'public') THEN
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'administradores' AND column_name = 'id' AND table_schema = 'public') THEN
            IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname = 'public' AND sequencename = 'administradores_id_seq') THEN
                CREATE SEQUENCE public.administradores_id_seq;
            END IF;
            PERFORM setval('public.administradores_id_seq', GREATEST((SELECT COALESCE(MAX(id), 0) FROM public.administradores), 1));
            ALTER TABLE public.administradores ALTER COLUMN id SET DEFAULT nextval('public.administradores_id_seq');
            ALTER SEQUENCE public.administradores_id_seq OWNED BY public.administradores.id;
            ALTER TABLE public.administradores ADD PRIMARY KEY (id);
            RAISE NOTICE '[PK] administradores.id marcada como PRIMARY KEY';
        END IF;
    ELSE
        RAISE NOTICE '[SKIP] administradores ya tiene PRIMARY KEY';
    END IF;

    RAISE NOTICE '=== Script v3 completado ===';
END;
$$;

-- ============================================================
-- Verificacion final: todas las tablas con su estado de PK
-- ============================================================
SELECT
    t2.table_name,
    COALESCE(
        (SELECT k.column_name
         FROM information_schema.key_column_usage k
         JOIN information_schema.table_constraints tc
           ON tc.constraint_name = k.constraint_name AND tc.table_schema = k.table_schema
         WHERE tc.table_name = t2.table_name
           AND tc.constraint_type = 'PRIMARY KEY'
           AND tc.table_schema = 'public'
         LIMIT 1),
        '*** SIN PRIMARY KEY ***'
    ) AS columna_pk,
    COALESCE(
        (SELECT c.column_default
         FROM information_schema.key_column_usage k
         JOIN information_schema.table_constraints tc
           ON tc.constraint_name = k.constraint_name AND tc.table_schema = k.table_schema
         JOIN information_schema.columns c
           ON c.table_name = k.table_name AND c.column_name = k.column_name AND c.table_schema = 'public'
         WHERE tc.table_name = t2.table_name
           AND tc.constraint_type = 'PRIMARY KEY'
           AND tc.table_schema = 'public'
         LIMIT 1),
        '*** SIN DEFAULT (secuencia) ***'
    ) AS default_pk
FROM information_schema.tables t2
WHERE t2.table_schema = 'public'
  AND t2.table_type   = 'BASE TABLE'
ORDER BY t2.table_name;
