-- ============================================================
-- Comercio.NET Web - Esquema de Base de Datos PostgreSQL
-- Ejecutar con: psql -U postgres -d comercio -f crear-base-datos.sql
-- ============================================================

-- Tabla de usuarios del sistema
CREATE TABLE IF NOT EXISTS usuarios (
    idusuario SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    usuario VARCHAR(50) NOT NULL UNIQUE,
    clave VARCHAR(255) NOT NULL,
    rol VARCHAR(50) DEFAULT 'Cajero',
    activo BOOLEAN DEFAULT true,
    numerocajero INTEGER DEFAULT 1
);

-- Tabla de productos
CREATE TABLE IF NOT EXISTS productos (
    idproducto SERIAL PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL UNIQUE,
    descripcion VARCHAR(255) NOT NULL,
    precio DECIMAL(18,2) NOT NULL DEFAULT 0,
    costo DECIMAL(18,2) DEFAULT 0,
    cantidad INTEGER DEFAULT 0,
    rubro VARCHAR(100) DEFAULT '',
    marca VARCHAR(100) DEFAULT '',
    proveedor VARCHAR(100) DEFAULT '',
    porcentajeiva DECIMAL(5,2) DEFAULT 21,
    activo BOOLEAN DEFAULT true,
    fechaalta TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabla de ventas (items de tickets en curso y cerrados)
CREATE TABLE IF NOT EXISTS ventas (
    idventa SERIAL PRIMARY KEY,
    nrofactura INTEGER NOT NULL,
    codigo VARCHAR(50) NOT NULL,
    descripcion VARCHAR(255) NOT NULL,
    cantidad INTEGER NOT NULL DEFAULT 1,
    precio DECIMAL(18,2) NOT NULL,
    total DECIMAL(18,2) NOT NULL,
    costo DECIMAL(18,2) DEFAULT 0,
    rubro VARCHAR(100) DEFAULT '',
    marca VARCHAR(100) DEFAULT '',
    proveedor VARCHAR(100) DEFAULT '',
    porcentajeiva DECIMAL(5,2) DEFAULT 21,
    descuentoaplicado DECIMAL(5,2) DEFAULT 0,
    esoferta BOOLEAN DEFAULT false,
    fecha TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabla de facturas (tickets cerrados/cobrados)
CREATE TABLE IF NOT EXISTS facturas (
    idfactura SERIAL PRIMARY KEY,
    numeroremito INTEGER NOT NULL,
    formadepago VARCHAR(50) NOT NULL,
    tipofactura VARCHAR(50) DEFAULT 'Remito',
    cajero VARCHAR(50) DEFAULT '',
    usuarioventa VARCHAR(100) DEFAULT '',
    fecha DATE DEFAULT CURRENT_DATE,
    hora TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    importetotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    importefinal DECIMAL(18,2) NOT NULL DEFAULT 0,
    esctacte BIT DEFAULT '0',
    ctactenombre VARCHAR(100) DEFAULT NULL,
    cuitcliente VARCHAR(20) DEFAULT NULL,
    iva DECIMAL(18,2) DEFAULT 0,
    porcentajedescuento DECIMAL(5,2) DEFAULT 0,
    importedescuento DECIMAL(18,2) DEFAULT 0,
    nrofactura VARCHAR(50) DEFAULT NULL,
    caenumero VARCHAR(50) DEFAULT NULL,
    caevencimiento VARCHAR(20) DEFAULT NULL
);

-- Tabla de detalle de pagos por factura
CREATE TABLE IF NOT EXISTS detallespagofactura (
    iddetalle SERIAL PRIMARY KEY,
    idfactura INTEGER REFERENCES facturas(idfactura),
    mediopago VARCHAR(50) NOT NULL,
    importe DECIMAL(18,2) NOT NULL,
    observaciones TEXT DEFAULT NULL,
    fecha TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    usuario VARCHAR(100) DEFAULT '',
    nroremito INTEGER DEFAULT 0
);

-- Tabla de numero de remito (secuencia)
CREATE TABLE IF NOT EXISTS numeroremito (
    id SERIAL PRIMARY KEY,
    ultimo INTEGER NOT NULL DEFAULT 0
);

-- Inicializar secuencia de remitos si esta vacia
INSERT INTO numeroremito (ultimo) 
SELECT 0 WHERE NOT EXISTS (SELECT 1 FROM numeroremito);

-- Tabla de turnos de cajero
CREATE TABLE IF NOT EXISTS turnoscajero (
    idturno SERIAL PRIMARY KEY,
    cajero VARCHAR(50) NOT NULL,
    usuario VARCHAR(100) DEFAULT '',
    fechaapertura TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fechacierre TIMESTAMP DEFAULT NULL,
    montoinicial DECIMAL(18,2) DEFAULT 0,
    montofinal DECIMAL(18,2) DEFAULT 0,
    observaciones TEXT DEFAULT '',
    activo BOOLEAN DEFAULT true
);

-- Tabla de auditoria de productos eliminados
CREATE TABLE IF NOT EXISTS auditoriaproductoseliminados (
    id SERIAL PRIMARY KEY,
    nroremito INTEGER NOT NULL,
    codigo VARCHAR(50) DEFAULT '',
    descripcion VARCHAR(255) DEFAULT '',
    cantidad INTEGER DEFAULT 0,
    precio DECIMAL(18,2) DEFAULT 0,
    total DECIMAL(18,2) DEFAULT 0,
    usuario VARCHAR(100) DEFAULT '',
    cajero VARCHAR(50) DEFAULT '',
    motivo VARCHAR(255) DEFAULT 'Eliminado por usuario',
    fecha TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabla de pagos a proveedores
CREATE TABLE IF NOT EXISTS pagosproveedores (
    id SERIAL PRIMARY KEY,
    proveedor VARCHAR(255) NOT NULL,
    importe DECIMAL(18,2) NOT NULL,
    fecha DATE DEFAULT CURRENT_DATE,
    hora TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    cajero VARCHAR(50) DEFAULT '',
    usuario VARCHAR(100) DEFAULT '',
    observaciones TEXT DEFAULT ''
);

-- ============================================================
-- Indices para mejorar rendimiento
-- ============================================================
CREATE INDEX IF NOT EXISTS idx_ventas_nrofactura ON ventas(nrofactura);
CREATE INDEX IF NOT EXISTS idx_facturas_numeroremito ON facturas(numeroremito);
CREATE INDEX IF NOT EXISTS idx_facturas_fecha ON facturas(fecha);
CREATE INDEX IF NOT EXISTS idx_productos_codigo ON productos(codigo);
CREATE INDEX IF NOT EXISTS idx_detallespago_idfactura ON detallespagofactura(idfactura);
CREATE INDEX IF NOT EXISTS idx_turnoscajero_activo ON turnoscajero(activo);

-- ============================================================
-- Usuario administrador por defecto
-- Contrasena: admin (cambiar despues del primer inicio)
-- ============================================================
INSERT INTO usuarios (nombre, usuario, clave, rol, activo, numerocajero)
SELECT 'Administrador', 'admin', 'admin', 'Administrador', true, 1
WHERE NOT EXISTS (SELECT 1 FROM usuarios WHERE usuario = 'admin');

-- ============================================================
-- FIN DEL ESQUEMA
-- ============================================================
