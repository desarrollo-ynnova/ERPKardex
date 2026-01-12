-- USE DB
use erp_kardex_dev;

drop table if exists stock_almacen;
drop table if exists empresa;
drop table if exists sucursal;
drop table if exists almacen;
drop table if exists motivo;
drop table if exists centro_costo;
drop table if exists actividad;
drop table if exists moneda;
drop table if exists tipo_documento;
drop table if exists ingresosalidaalm;
drop table if exists dingresosalidaalm;
drop table if exists grupo;
drop table if exists subgrupo;
drop table if exists cuenta;
drop table if exists unidad_medida;
drop table if exists formulacion_quimica;
drop table if exists peligrosidad;
drop table if exists producto;
drop table if exists ingrediente_activo;
drop table if exists detalle_ingrediente_activo;
drop table if exists marca;
drop table if exists modelo;
drop table if exists estado;
drop table if exists usuario;
drop table if exists tipo_usuario;
drop table if exists empresa_usuario;
drop table if exists entidad;
drop table if exists tipo_existencia;
drop table if exists dpedservicio;
drop table if exists pedservicio;
drop table if exists dpedcompra;
drop table if exists pedcompra;
drop table if exists tipo_documento_interno;
drop table if exists dreqservicio;
drop table if exists reqservicio;
drop table if exists dreqcompra;
drop table if exists reqcompra;
drop table if exists ordencompra;
drop table if exists dordencompra;
drop table if exists ordenservicio;
drop table if exists dordenservicio;

GO

CREATE TABLE tipo_documento_interno (
    id INT IDENTITY(1,1) PRIMARY KEY,
    codigo VARCHAR(20),      -- Ej: PED, PS, REQ, NI (Nota Ingreso)
    descripcion VARCHAR(200),
    ultimo_correlativo INT DEFAULT 0, -- Para llevar el control del número actual (ej. va en el 150)
    estado BIT DEFAULT 1
);

CREATE TABLE tipo_usuario (
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(255),
    estado BIT DEFAULT 1
);

-- Tabla de Usuarios (Globales, sin empresa_id aquí)
CREATE TABLE usuario (
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    dni CHAR(8) NOT NULL,
    nombre VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    telefono VARCHAR(20),
    password VARCHAR(255) NOT NULL,
    estado BIT NOT NULL DEFAULT 1
);

-- Tabla Intermedia (Relación N a N Manual)
CREATE TABLE empresa_usuario (
    id INT IDENTITY(1,1) PRIMARY KEY,
    empresa_id INT NOT NULL,      -- Relación lógica con tabla empresa
    usuario_id INT NOT NULL,      -- Relación lógica con tabla usuario
    tipo_usuario_id INT NOT NULL, -- Relación lógica con tabla tipo_usuario
    estado BIT DEFAULT 1
);

create table estado (
	id INT IDENTITY(1,1) PRIMARY KEY,
	nombre varchar(255),
	tabla varchar(255)
);

create table empresa (
	id INT IDENTITY(1,1) PRIMARY KEY,
	ruc char(11),
	razon_social varchar(255),
	estado BIT,
);

create table sucursal (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	nombre varchar(255),
	estado BIT,
	empresa_id INT,
);

create table almacen (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo VARCHAR(255),
	nombre varchar(255),
	estado BIT,
	sucursal_id INT,
	cod_sucursal varchar(255),
	es_valorizado BIT,
	empresa_id int
);

create table moneda (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	nombre varchar(255),
	estado BIT
);

create table motivo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	tipo_movimiento BIT, -- 1: INGRESO, 0: SALIDA
	descripcion VARCHAR(255),
	estado BIT
);

create table centro_costo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    codigo VARCHAR(20),
    nombre VARCHAR(255),
    empresa_id INT,
    padre_id INT,
    es_imputable BIT DEFAULT 1,
	estado BIT DEFAULT 1,
    fecha_registro DATETIME DEFAULT GETDATE(),
);

create table actividad (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	nombre varchar(255),
	estado BIT,
	empresa_id INT,
);

create table tipo_documento (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	descripcion varchar(max),
	estado BIT
);

CREATE TABLE entidad (
	id INT IDENTITY(1,1) PRIMARY KEY,
	ruc varchar(255),
	razon_social varchar(255),
	nombre_contacto varchar(255),
    telefono varchar(255),
    email varchar(255),
	estado BIT,
	empresa_id INT,
);

create table ingresosalidaalm (
	id INT IDENTITY(1,1) PRIMARY KEY,
	-- Referencia al Tipo de Documento Interno (IALM / SALM)
    tipo_documento_interno_id INT,
	fecha DATE,
	numero varchar(255),
	sucursal_id INT,
	almacen_id INT,
	tipo_movimiento BIT,
	motivo_id INT,
	fecha_documento DATE,
	tipo_documento_id int,
	serie_documento varchar(255),
	numero_documento varchar(255),
	fecha_documento_valorizacion DATE,
	tipo_documento_valorizacion_id int,
	serie_documento_valorizacion varchar(255),
	numero_documento_valorizacion varchar(255),
	moneda_id int,
	estado_id int,
	usuario_id INT,
	fecha_registro DATETIME DEFAULT GETDATE(),
	empresa_id INT,
	entidad_id INT,
);

create table dingresosalidaalm (
	id INT IDENTITY(1,1) PRIMARY KEY,
	ingresosalidaalm_id INT,
	item varchar(255),
	producto_id INT,
	cod_producto varchar(255),
	descripcion_producto varchar(255),
	cod_unidad_medida varchar(255),
	cantidad decimal(12,2),
	tipo_documento_id int,
	fecha_documento DATE,
	serie_documento varchar(255),
	numero_documento varchar(255),
	moneda_id int,
	tipo_cambio decimal(12,6),
	precio decimal(19,6),
	igv decimal(19,6),
	subtotal decimal(19,6),
	total decimal(19,6),
	centro_costo_id int,
	actividad_id int,
	usuario_id INT,
	fecha_registro DATETIME DEFAULT GETDATE(),
	empresa_id INT,
);

create table tipo_existencia (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	nombre varchar(255),
	estado BIT
);

create table cuenta (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	descripcion varchar(200),
	empresa_id INT,
);

create table grupo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	descripcion varchar(200),
	cuenta_id INT,
	empresa_id INT,
	tipo_existencia_id INT
);

create table subgrupo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	descripcion varchar(200),
	grupo_id INT,
	cod_grupo varchar(255),
	descripcion_grupo varchar(255),
	observacion varchar(255),
	empresa_id INT
);

create table unidad_medida (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200),
);

create table formulacion_quimica (
	codigo varchar(255) PRIMARY KEY,
	nombre varchar(255),
	descripcion varchar(255),
	ejemplo varchar (255)
);

create table peligrosidad (
	codigo varchar(255) PRIMARY KEY,
	clase varchar(255),
	banda_color varchar(255),
	descripcion varchar(255),
	nivel_riesgo varchar(255),
	uso_senasa BIT
);

create table marca (
	id INT IDENTITY(1,1) PRIMARY KEY,
	nombre varchar(255),
	estado BIT,
	empresa_id INT
);

create table modelo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	nombre varchar(255),
	estado BIT,
	marca_id INT,
	empresa_id INT
);

create table producto (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	grupo_id INT,
	cod_grupo varchar(255),
	descripcion_grupo varchar(255),
	subgrupo_id INT,
	cod_subgrupo varchar(255),
	descripcion_subgrupo varchar(255),
	descripcion_producto varchar(255),
	descripcion_comercial varchar(255),
	concentracion decimal(12,2),
	cod_formulacion_quimica varchar(255),
	lote varchar(255),
	fecha_fabricacion date,
	fecha_vencimiento date,
	cod_peligrosidad varchar(255),
	cod_unidad_medida varchar(255),
	marca_id INT,
	modelo_id INT,
	serie varchar(255),
	es_activo_fijo BIT,
	estado BIT, -- 1: activo 0: inactivo
	empresa_id INT
);

create table ingrediente_activo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	descripcion varchar(255),
	empresa_id INT
);

create table detalle_ingrediente_activo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	cod_producto varchar(255),
	ingrediente_activo_id int,
	porcentaje decimal(12,2)
);

CREATE TABLE stock_almacen (
    id INT IDENTITY(1,1) PRIMARY KEY,
    almacen_id INT NOT NULL,
    producto_id INT NOT NULL,
    stock_actual DECIMAL(12,2) DEFAULT 0,
	empresa_id INT,
    ultima_actualizacion DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_Stock_Almacen UNIQUE (almacen_id, producto_id, empresa_id)
);

-- 3.1 REQUERIMIENTO DE COMPRA
CREATE TABLE reqcompra (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_interno_id INT, 
    numero VARCHAR(20),
    fecha_emision DATE,
    fecha_necesaria DATE,
    
    usuario_solicitante_id INT, -- Solo quién pide
    observacion VARCHAR(500),
    estado_id INT,              -- Solo: Pendiente, Aprobado, Rechazado
    
    empresa_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

CREATE TABLE dreqcompra (
    id INT IDENTITY(1,1) PRIMARY KEY,
    reqcompra_id INT,
    item CHAR(3),                  
    
    producto_id INT,
	centro_costo_id INT,
    descripcion_producto VARCHAR(500), -- Snapshot nombre
    unidad_medida VARCHAR(50),         -- Snapshot unidad
    
    cantidad_solicitada DECIMAL(12,2),
    
    lugar VARCHAR(255),
    empresa_id INT
);

-- 3.2 REQUERIMIENTO DE SERVICIO
CREATE TABLE reqservicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_interno_id INT, 
    numero VARCHAR(20),
    fecha_emision DATE,
    fecha_necesaria DATE,
    
    usuario_solicitante_id INT,
    observacion VARCHAR(500),
    estado_id INT,              -- Solo: Pendiente, Aprobado, Rechazado
    
    empresa_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

CREATE TABLE dreqservicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    reqservicio_id INT,
    item CHAR(3),
    
    producto_id INT,         
	centro_costo_id INT,
    descripcion_servicio VARCHAR(MAX),
    unidad_medida VARCHAR(50),
    
    cantidad_solicitada DECIMAL(12,2) DEFAULT 1,
    
    lugar VARCHAR(255),
    empresa_id INT
);

-- =========================================================================
-- 4. TABLAS MODIFICADAS: PEDIDOS (EL CONSOLIDADOR)
-- =========================================================================

-- 4.1 PEDIDO DE COMPRA (PED) - ATIENDE REQ
CREATE TABLE pedcompra (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_interno_id INT, -- Referencia a 'PED'
    numero VARCHAR(20),            -- Ej: 'PED-00001'
    
    fecha_emision DATE,
    fecha_necesaria DATE,
    
    lugar_destino VARCHAR(255),
	sucursal_id INT,
	almacen_id INT,

    usuario_solicitante_id INT,    -- Quien procesa el pedido
    
    observacion VARCHAR(500),
    estado_id INT,
    
    empresa_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

CREATE TABLE dpedcompra (
    id INT IDENTITY(1,1) PRIMARY KEY,
    pedcompra_id INT,
    item CHAR(3),
    
    producto_id INT,
	centro_costo_id INT,           -- Opcional, ya que viene del REQ
    descripcion_libre VARCHAR(500), 
    unidad_medida VARCHAR(50),
    
    cantidad_solicitada DECIMAL(12,2), -- Cantidad que se está pidiendo/atendiendo ahora
    cantidad_aprobada DECIMAL(12,2),   -- Si hubiera flujo de aprobación de la orden
	cantidad_atendida DECIMAL(12,2) DEFAULT 0,
    
    -- COLUMNAS DE REFERENCIA (LO QUE PEDISTE)
    id_referencia INT,             -- ID del detalle origen (dreqcompra.id)
    tabla_referencia VARCHAR(50),  -- 'DREQCOMPRA'
    item_referencia VARCHAR(10),   -- Item del requerimiento origen (ej: '001')
    
    lugar VARCHAR(255),
    observacion_item VARCHAR(255),
    empresa_id INT
);

-- 4.2 PEDIDO DE SERVICIO (PS) - ATIENDE RS
CREATE TABLE pedservicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_interno_id INT, -- Referencia a 'PS'
    numero VARCHAR(20),            -- Ej: 'PS-00001'
    
    fecha_emision DATE,
    fecha_necesaria DATE,

	lugar_destino VARCHAR(255),
	sucursal_id INT,
	almacen_id INT,

    
    usuario_solicitante_id INT,
    observacion VARCHAR(500),
    estado_id INT,
    
    empresa_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

CREATE TABLE dpedservicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    pedservicio_id INT,
    item CHAR(3),
    producto_id INT,
	centro_costo_id INT,
    
    descripcion_servicio VARCHAR(MAX),
    cantidad DECIMAL(12,2) DEFAULT 1,
	cantidad_atendida DECIMAL(12,2) DEFAULT 0,
    unidad_medida VARCHAR(50),
    
    -- COLUMNAS DE REFERENCIA (LO QUE PEDISTE)
    id_referencia INT,             -- ID del detalle origen (dreqservicio.id)
    tabla_referencia VARCHAR(50),  -- 'DREQSERVICIO'
    item_referencia VARCHAR(10),   -- Item del requerimiento origen (ej: '001')
    
    lugar VARCHAR(255),
    observacion_item VARCHAR(255),
    empresa_id INT
);

CREATE TABLE ordencompra (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_interno_id INT, -- 'OCO'
    numero VARCHAR(20),            -- Ej: OCO-0000001
    
    -- Datos del Proveedor (Vital para la Orden)
    entidad_id INT,                -- Proveedor seleccionado
    
    fecha_emision DATE,
    fecha_entrega DATE,            -- NISIRA: Plazo de Entrega
    
    moneda_id INT,                 -- NISIRA: Moneda
    tipo_cambio DECIMAL(12,6),     -- NISIRA: T.Cambio
    
    condicion_pago VARCHAR(255),   -- NISIRA: Condiciones (Ej: Crédito 30 días)
    lugar_destino VARCHAR(255),    -- NISIRA: Lugar Entrega
    
    sucursal_id INT,               -- Para recepción de mercadería
    almacen_id INT,
    
    observacion VARCHAR(500),
    incluye_igv BIT DEFAULT 1,     -- Para cálculo de impuestos
    
    -- Importes Totales
    total_afecto DECIMAL(18,2) DEFAULT 0,
    total_inafecto DECIMAL(18,2) DEFAULT 0,
    igv_total DECIMAL(18,2) DEFAULT 0,
    total DECIMAL(18,2) DEFAULT 0,

    estado_id INT,
    usuario_creacion_id INT,
    empresa_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

-- 3. TABLA: DETALLE ORDEN DE COMPRA
-- Basado en NISIRA Pág. 82 (Items, Cantidad, Precio, Descuentos)
CREATE TABLE dordencompra (
    id INT IDENTITY(1,1) PRIMARY KEY,
    ordencompra_id INT,
    
    item CHAR(3),
    producto_id INT,
    
    -- Snapshots (Datos copiados del maestro al momento de la orden)
    descripcion VARCHAR(500),
    unidad_medida VARCHAR(50),
    
    -- Cantidades
    cantidad DECIMAL(12,2),        -- La cantidad que FINALMENTE se compra
    
    -- Valores Monetarios (Lo que negociaste con el proveedor)
    precio_unitario DECIMAL(18,6), -- NISIRA: P.Unitario
    porc_descuento DECIMAL(12,2) DEFAULT 0, -- NISIRA: %Dscto
    
    valor_venta DECIMAL(18,2),     -- Subtotal sin impuestos
    impuesto DECIMAL(18,2),        -- IGV del ítem
    total DECIMAL(18,2),           -- Total con impuestos
    
    centro_costo_id INT,           -- NISIRA: Destino/Centro de Costo
    lugar VARCHAR(255),
    -- TRAZABILIDAD (La clave para no "rayarse" con los pedidos)
    id_referencia INT,             -- ID de dpedcompra
    tabla_referencia VARCHAR(50) DEFAULT 'DPEDCOMPRA',
    
    observacion_item VARCHAR(255),
    empresa_id INT
);

-- 4. TABLA: ORDEN DE SERVICIO (CABECERA)
-- Basado en NISIRA Pág. 89 (Similar a Compra, pero orientado a servicios)
CREATE TABLE ordenservicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_interno_id INT, -- 'OS'
    numero VARCHAR(20),
    
    entidad_id INT,                -- Proveedor del Servicio
    
    fecha_emision DATE,
    fecha_inicio_servicio DATE,    -- NISIRA: Fecha Inicio
    fecha_fin_servicio DATE,       -- NISIRA: Fecha Fin
    
    moneda_id INT,
    tipo_cambio DECIMAL(12,6),
    
    condicion_pago VARCHAR(255),
    lugar_destino VARCHAR(255),    -- Lugar donde se realiza el servicio
    sucursal_id INT,               -- Sucursal contable/administrativa
    
    observacion VARCHAR(500),
    incluye_igv BIT DEFAULT 1,
    
    total_afecto DECIMAL(18,2) DEFAULT 0,
    total_inafecto DECIMAL(18,2) DEFAULT 0,
    igv_total DECIMAL(18,2) DEFAULT 0,
    total DECIMAL(18,2) DEFAULT 0, -- NISIRA: Total Servicio

    estado_id INT,
    usuario_creacion_id INT,
    empresa_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

-- 5. TABLA: DETALLE ORDEN DE SERVICIO
CREATE TABLE dordenservicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    ordenservicio_id INT,
    
    item CHAR(3),
    producto_id INT,               -- Servicio catalogado
    
    descripcion VARCHAR(MAX),      -- Descripción detallada del trabajo
    unidad_medida VARCHAR(50),
    
    cantidad DECIMAL(12,2),
    precio_unitario DECIMAL(18,6),
    
    valor_venta DECIMAL(18,2),
    impuesto DECIMAL(18,2),
    total DECIMAL(18,2),
    
    centro_costo_id INT,
    lugar VARCHAR(255),

    -- TRAZABILIDAD
    id_referencia INT,             -- ID de dpedservicio
    tabla_referencia VARCHAR(50) DEFAULT 'DPEDSERVICIO',
    
    empresa_id INT
);

GO

-- ==========================================
-- 5. DATOS DE CONFIGURACIÓN INICIAL
-- ==========================================

INSERT INTO tipo_documento_interno (codigo, descripcion, ultimo_correlativo) VALUES 
('IALM', 'NOTA DE INGRESO ALMACEN', 0),
('SALM', 'NOTA DE SALIDA ALMACEN', 0),
('REQ',  'REQUERIMIENTO DE COMPRA', 0),
('RS',   'REQUERIMIENTO DE SERVICIO', 0),
('PED',  'PEDIDO DE COMPRA', 0),
('PS',   'PEDIDO DE SERVICIO', 0),
('OCO',  'ORDEN DE COMPRA', 0),
('OS',  'ORDEN DE SERVICIO', 0);

-- inserts de 'estado'
INSERT INTO estado (nombre, tabla) VALUES ('Aprobado', 'INGRESOSALIDAALM');

-- Solo los estados que pediste para los REQUERIMIENTOS
INSERT INTO estado (nombre, tabla) VALUES 
('Pendiente', 'REQ'),
('Aprobado', 'REQ'),
('Atendido', 'REQ'),
('Rechazado', 'REQ');

-- Estados para el Pedido (Operativos)
INSERT INTO estado (nombre, tabla) VALUES 
('Generado', 'PED'),
('Anulado', 'PED'),
('Atendido Parcial', 'PED'),
('Atendido Total', 'PED');

INSERT INTO estado (nombre, tabla) VALUES ('Generado', 'ORDEN');
INSERT INTO estado (nombre, tabla) VALUES ('Anulado', 'ORDEN');
INSERT INTO estado (nombre, tabla) VALUES ('Aprobado', 'ORDEN');

GO

-- inserts de 'unidad_medida'
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('4A','BOBINAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BJ','BALDE');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BLL','BARRILES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BG','BOLSA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BO','BOTELLAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BX','CAJA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CT','CARTONES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CMK','CENTIMETRO CUADRADO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CMQ','CENTIMETRO CUBICO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CMT','CENTIMETRO LINEAL');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CEN','CIENTO DE UNIDADES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CY','CILINDRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CJ','CONOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('DZN','DOCENA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('DZP','DOCENA POR 10**6');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('BE','FARDO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('GLI','GALON INGLES (4,545956L)');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('GRM','GRAMO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('GRO','GRUESA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('HLT','HECTOLITRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('LEF','HOJA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('SET','JUEGO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('KGM','KILOGRAMO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('KTM','KILOMETRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('KWH','KILOVATIO HORA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('KT','KIT');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('CA','LATAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('LBR','LIBRAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('LTR','LITRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MWH','MEGAWATT HORA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MTR','METRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MTK','METRO CUADRADO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MTQ','METRO CUBICO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MGM','MILIGRAMOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MLT','MILILITRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MMT','MILIMETRO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MMK','MILIMETRO CUADRADO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MMQ','MILIMETRO CUBICO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('MLL','MILLARES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('UM','MILLON DE UNIDADES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('ONZ','ONZAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('PF','PALETAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('PK','PAQUETE');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('PR','PAR');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('FOT','PIES');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('FTK','PIES CUADRADOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('FTQ','PIES CUBICOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('C62','PIEZAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('PG','PLACAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('ST','PLIEGO');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('INH','PULGADAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('RM','RESMA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('DR','TAMBOR');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('STN','TONELADA CORTA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('LTN','TONELADA LARGA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('TNE','TONELADAS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('TU','TUBOS');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('NIU','UNIDAD (BIENES)');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('ZZ','UNIDAD (SERVICIOS)');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('GLL','US GALON (3,7843 L)');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('YRD','YARDA');
INSERT INTO unidad_medida (codigo, descripcion) VALUES ('YDK','YARDA CUADRADA');

-- inserts de 'cuenta'
INSERT INTO cuenta (codigo, descripcion, empresa_id) VALUES ('21', 'MERCADERÍAS', 1);
INSERT INTO cuenta (codigo, descripcion, empresa_id) VALUES ('24', 'MATERIALES SUMINISTROS Y REPUESTOS', 1);
INSERT INTO cuenta (codigo, descripcion, empresa_id) VALUES ('33', 'ACTIVOS', 1);
INSERT INTO cuenta (codigo, descripcion, empresa_id) VALUES ('63', 'SERVICIOS', 1);

-- inserts de 'peligrosidad'
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-IA','IA','ROJO INTENSO','EXTREMADAMENTE PELIGROSO','MUY ALTO',1);
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-IB','IB','ROJO','ALTAMENTE PELIGROSO','ALTO',1);
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-II','II','AMARILLO','MODERADAMENTE PELIGROSO','MEDIO',1);
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-III','III','AZUL','LIGERAMENTE PELIGROSO','BAJO',1);
INSERT INTO peligrosidad (codigo, clase, banda_color, descripcion, nivel_riesgo, uso_senasa) VALUES ('OMS-U','U','VERDE','IMPROBABLE QUE PRESENTE PELIGRO','MUY BAJO',1);

-- inserts de 'formulacion_quimica'
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('EC','EMULSIFIABLE CONCENTRATE','INGREDIENTE ACTIVO DISUELTO EN SOLVENTE ORGÁNICO + EMULSIFICANTES','CLORPIRIFOS 48% EC');
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('SC','SUSPENSION CONCENTRATE','SÓLIDOS FINOS SUSPENDIDOS EN AGUA','IMIDACLOPRID 35% SC');
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('SL','SOLUBLE LIQUID','INGREDIENTE ACTIVO TOTALMENTE SOLUBLE EN AGUA','GLIFOSATO 48% SL');
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('EW','EMULSION, OIL IN WATER','EMULSIÓN ACEITE EN AGUA (MENOS SOLVENTE)','PIRETROIDES EW');
INSERT INTO formulacion_quimica (codigo, nombre, descripcion, ejemplo) VALUES ('CS','CAPSULE SUSPENSION','MICROCÁPSULAS SUSPENDIDAS','LAMBDA-CIHALOTRINA CS');

-- inserts de 'empresa'
INSERT INTO empresa (ruc, razon_social, estado) VALUES ('20607778338', 'CONTROL SCIENCE DEL PERU S.A.C.', 1);
INSERT INTO empresa (ruc, razon_social, estado) VALUES ('20603727551', 'STALNO S.A.C', 1);
INSERT INTO empresa (ruc, razon_social, estado) VALUES ('20613898167', 'MAQUINARIA Y SANIDAD AGRÍCOLA S.A.C.', 1);
INSERT INTO empresa (ruc, razon_social, estado) VALUES ('20615184153', 'SUPPLY BIOTECHNOLOGY LOGISTIC WORLD S.A.C.S.', 1);

-- inserts de 'sucursal'
INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('001', 'PRINCIPAL - POMALCA', 1, 1);

-- inserts de 'almacen'
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, sucursal_id, es_valorizado, empresa_id) VALUES ('01','PRINCIPAL',1,'001', 1, 1, 1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, sucursal_id, es_valorizado, empresa_id) VALUES ('02','TERCEROS',1,'001', 1, 1, 1);

-- inserts de 'tipo_documento'
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('01','Factura',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('02','Recibo por Honorarios',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('03','Boleta de Venta',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('04','Liquidación de compra',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('05','Boletos de Transporte Aéreo',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('06','Carta de porte aéreo por el servicio de transporte de carga aérea',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('07','Nota de crédito',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('08','Nota de débito',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('09','Guía de remisión - Remitente',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('10','Recibo por Arrendamiento',1);

-- inserts de 'moneda'
INSERT INTO moneda (codigo, nombre, estado) VALUES ('01', 'SOLES', 1);
INSERT INTO moneda (codigo, nombre, estado) VALUES ('02', 'DÓLARES', 1);

-- inserts de 'motivo'
-- tipo_movimiento: 1 - ENTRADAS
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('02',1,'COMPRA NACIONAL',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('03',1,'CONSIGNACIÓN RECIBIDA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('05',1,'DEVOLUCIÓN RECIBIDA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('07',1,'BONIFICACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('08',1,'PREMIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('09',1,'DONACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('16',1,'SALDO INICIAL',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('18',1,'IMPORTACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('19',1,'ENTRADA DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('20',1,'ENTRADA POR DEVOLUCIÓN DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('21',1,'ENTRADA POR TRANSFERENCIA ENTRE ALMACENES ',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('22',1,'ENTRADA POR IDENTIFICACIÓN ERRONEA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('24',1,'ENTRADA POR DEVOLUCIÓN DEL CLIENTE',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('26',1,'ENTRADA PARA SERVICIO DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('28',1,'AJUSTE POR DIFERENCIA DE INVENTARIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('29',1,'ENTRADA DE BIENES EN PRÉSTAMO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('31',1,'ENTRADA DE BIENES EN CUSTODIA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('33',1,'MUESTRAS MÉDICAS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('34',1,'PUBLICIDAD',1);
-- tipo_movimiento: 0 - SALIDAS
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('01',0,'VENTA NACIONAL',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('04',0,'CONSIGNACIÓN ENTREGADA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('06',0,'DEVOLUCIÓN ENTREGADA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('07',0,'BONIFICACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('08',0,'PREMIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('09',0,'DONACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('10',0,'SALIDA A PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('11',0,'SALIDA POR TRANSFERENCIA ENTRE ALMACENES ',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('12',0,'RETIRO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('13',0,'MERMAS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('14',0,'DESMEDROS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('15',0,'DESTRUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('17',0,'EXPORTACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('23',0,'SALIDA POR IDENTIFICACIÓN ERRONEA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('25',0,'SALIDA POR DEVOLUCIÓN AL PROVEEDOR',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('27',0,'SALIDA POR SERVICIO DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('28',0,'AJUSTE POR DIFERENCIA DE INVENTARIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('30',0,'SALIDA DE BIENES EN PRÉSTAMO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('32',0,'SALIDA DE BIENES EN CUSTODIA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('33',0,'MUESTRAS MÉDICAS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('34',0,'PUBLICIDAD',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('36',0,'RETIRO PARA ENTREGA A TRABAJADORES',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('37',0,'RETIRO POR CONVENIO COLECTIVO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('38',0,'RETIRO POR SUSTITUCIÓN DE BIEN SINIESTRADO',1);

-- inserts de 'centro_costo'
-- 1. NIVEL 1: Áreas Generales (Padres - No Imputables)
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES 
--('ADM', 'ADMINISTRACIÓN CENTRAL', 1, NULL, 0, 1),      -- ID 1
--('OP-AGRO', 'OPERACIONES AGRÍCOLAS', 1, NULL, 0, 1),   -- ID 2
--('PROD', 'PLANTA DE PRODUCCIÓN', 1, NULL, 0, 1),       -- ID 3
--('COM', 'COMERCIAL Y VENTAS', 1, NULL, 0, 1);          -- ID 4

-- NIVEL 1: PADRES RAÍZ
INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('C1101', 'TERRENOS', 1, NULL, 0, 1),
('C1102', 'ACTIVO FIJO', 1, NULL, 0, 1),
('C1103', 'GESTION ADMINISTRATIVA', 1, NULL, 0, 1),
('C1104', 'GESTION OPERATIVA', 1, NULL, 0, 1),
('C1105', 'GESTION VENTAS', 1, NULL, 0, 1);

-- NIVEL 2: HIJOS DIRECTOS
INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('C110101', 'TERRENO PROPIO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1101'), 1, 1),
('C110102', 'TERRENO ALQUILADO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1101'), 0, 1),
('C110201', 'INFRAESTRUCTURA', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
('C110202', 'MAQUINARIA Y EQUIPOS', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
('C110203', 'EQUIPOS AUXILIARES', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
('C110204', 'VEHICULOS', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
('C110205', 'INTANGIBLES', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
('C110301', 'GERENCIA GENERAL', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1103'), 1, 1),
('C110401', 'PROCESO PRODUCTIVO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
('C110402', 'COMPRA DE INSUMOS', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
('C110404', 'INVESTIGACION Y DESARROLLO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
('C110405', 'CONTROL DE CALIDAD', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
('C110406', 'MANTENIMIENTO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
('C110501', 'VENTAS NACIONALES (PERU)', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1105'), 0, 1);

-- NIVEL 3: NIETOS
INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('C11010201', 'ALMACEN POMALCA', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110102'), 1, 1),
('C11050101', 'VENTA INSUMOS', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110501'), 1, 1),
('C11050102', 'SOPORTE POST-VENTA', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110501'), 1, 1),
('C11050103', 'ENSAYOS Y DEMOSTRACIONES', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110501'), 1, 1),
('C11050104', 'GESTION COMERCIAL - PERU', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110501'), 1, 1);

-- NIVEL 1: PADRES RAÍZ (empresa_id = 2)
INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('C0101', 'TERRENOS', 2, NULL, 0, 1),
('C0102', 'ACTIVO FIJO', 2, NULL, 0, 1),
('C0103', 'GESTION ADMINISTRATIVA', 2, NULL, 0, 1),
('C0104', 'GESTION OPERACIONES', 2, NULL, 0, 1),
('C0105', 'GESTION VENTAS', 2, NULL, 0, 1);

-- NIVEL 2: HIJOS DIRECTOS
INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('C010101', 'TERRENO PROPIO', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0101' AND empresa_id = 2), 1, 1),
('C010102', 'TERRENO ALQUILADO', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0101' AND empresa_id = 2), 1, 1),
('C010201', 'INFRAESTRUCTURA', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0102' AND empresa_id = 2), 1, 1),
('C010202', 'MAQUINARIA Y EQUIPOS', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0102' AND empresa_id = 2), 1, 1),
('C010203', 'EQUIPOS AUXILIARES', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0102' AND empresa_id = 2), 1, 1),
('C010204', 'VEHICULOS', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0102' AND empresa_id = 2), 0, 1),
('C010301', 'OFICINA DMINISTRATIVA', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0103' AND empresa_id = 2), 1, 1),
('C010302', 'GERENTE GENERAL', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0103' AND empresa_id = 2), 1, 1),
('C010401', 'SOLUCIONES INDUSTRIALES', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0104' AND empresa_id = 2), 1, 1),
('C010402', 'COMERCIALIZACION DE MATERIALES', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0104' AND empresa_id = 2), 1, 1),
('C010501', 'VENTAS Y COTIZACIONES', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1),
('C010502', 'MARKETING DIGITAL', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1),
('C010503', 'ATENCION AL CLIENTE', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1),
('C010504', 'POSTVENTA Y GARANTIAS', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1),
('C010505', 'GESTION COMERCIAL', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1);

-- NIVEL 3: NIETOS (Placas de vehículos)
INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('C01020401', 'FORD RANGER - PLACA M8J851', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C010204' AND empresa_id = 2), 1, 1),
('C01020402', 'RENAULT OROCH - PLACA M8K701', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C010204' AND empresa_id = 2), 1, 1);

-- NIVEL 1: PADRES RAÍZ (empresa_id = 3)
INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('C0301', 'TERRENOS', 3, NULL, 0, 1),
('C0302', 'ACTIVO FIJO', 3, NULL, 0, 1),
('C0303', 'GESTION ADMINISTRATIVA', 3, NULL, 0, 1),
('C0304', 'GESTION OPERACIONES', 3, NULL, 0, 1),
('C0305', 'GESTION VENTAS', 3, NULL, 0, 1);

-- NIVEL 2: HIJOS DIRECTOS
INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('C030101', 'TERRENO PROPIO', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0301' AND empresa_id = 3), 1, 1),
('C030102', 'TERRENO ALQUILADO', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0301' AND empresa_id = 3), 1, 1),
('C030201', 'INFRAESTRUCTURA', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0302' AND empresa_id = 3), 1, 1),
('C030202', 'MAQUINARIA Y EQUIPOS', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0302' AND empresa_id = 3), 1, 1),
('C030203', 'EQUIPOS AUXILIARES', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0302' AND empresa_id = 3), 1, 1),
('C030204', 'VEHICULOS', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0302' AND empresa_id = 3), 0, 1),
('C030301', 'OFICINA DMINISTRATIVA', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0303' AND empresa_id = 3), 1, 1),
('C030302', 'GERENTE GENERAL', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0303' AND empresa_id = 3), 1, 1),
('C030401', 'SERVICIOS DE MANTENIMIENTO', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0304' AND empresa_id = 3), 1, 1),
('C030402', 'COMERCIALIZACION DE MATERIALES', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0304' AND empresa_id = 3), 1, 1),
('C030501', 'VENTAS Y COTIZACIONES', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1),
('C030502', 'MARKETING DIGITAL', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1),
('C030503', 'ATENCION AL CLIENTE', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1),
('C030504', 'POSTVENTA Y GARANTIAS', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1),
('C030505', 'GESTION COMERCIAL', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1);

-- NIVEL 3: NIETOS
INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
('C03020401', 'FORD RANGER XLS - SIN PLACA', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C030204' AND empresa_id = 3), 1, 1);

-- inserts de 'actividad'
INSERT INTO actividad (codigo, nombre, estado, empresa_id) VALUES 
('001', 'RECEPCIÓN Y ALMACENAMIENTO', 1, 1),
('002', 'PASAJE Y DOSIFICACIÓN', 1, 1),
('003', 'MEZCLADO', 1, 1),
('004', 'ENVASADO', 1, 1),
('005', 'ETIQUETADO', 1, 1),
('006', 'DESPACHO', 1, 1),
('007', 'GESTIÓN OPERATIVA', 1, 1);

PRINT '>> Insertando Roles...';
INSERT INTO tipo_usuario (nombre, estado) VALUES ('ADMINISTRADOR DEL SISTEMA', 1); -- ID 1
INSERT INTO tipo_usuario (nombre, estado) VALUES ('LOGISTICO', 1);  -- ID 2
INSERT INTO tipo_usuario (nombre, estado) VALUES ('APROBADOR', 1);  -- ID 3
INSERT INTO tipo_usuario (nombre, estado) VALUES ('USUARIO', 1);       -- ID 4

INSERT INTO tipo_existencia (codigo, nombre, estado) 
VALUES 
('01', 'MERCADERÍA', 1),
('02', 'PRODUCTO TERMINADO', 1),
('03', 'MATERIAS PRIMAS Y AUXILIARES - MATERIALES', 1),
('04', 'ENVASES Y EMBALAJES', 1),
('05', 'SUMINISTROS DIVERSOS', 1),
('99', 'OTROS', 1);

PRINT '>> Insertando Usuario Alexis...';
INSERT INTO usuario (dni, nombre, email, telefono, password, estado) 
VALUES ('75090896', 'Alexis Torres Cabrejos', 'gfake040305@gmail.com', '999796517', 'password123', 1);

PRINT '>> Asignando Alexis a la Empresa 1...';
-- Obtenemos el ID del usuario recién creado para no fallar
DECLARE @NewUsuarioID INT = SCOPE_IDENTITY();

INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (1, @NewUsuarioID, 3, 1); -- Empresa 1, Rol 3 (Aprobador)

PRINT '>> Proceso de tablas de usuario finalizado.';
GO

USE erp_kardex_dev;
GO

-- ======================================================
-- 1. USUARIOS PARA EMPRESA_ID = 1 (CONTROL SCIENCE)
-- ======================================================
PRINT '>> Insertando usuarios para Empresa 1...';

-- James de la Cruz Calopino
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('74814548', 'James de la Cruz Calopino', 'jproduccion@agrosayans.com', '910467055', 'password123', 1);
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (1, SCOPE_IDENTITY(), 4, 1);

-- Lilyan Lozada Diaz
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('73138239', 'Lilyan Lozada Diaz', 'llozada@agrosayans.com', '930939954', 'password123', 1);
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (1, SCOPE_IDENTITY(), 4, 1);

-- Katherin Espinal Vasquez
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('75185380', 'Katherin Espinal Vasquez', 'kespinal@agrosayans.com', '977796697', 'password123', 1);
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (1, SCOPE_IDENTITY(), 4, 1);

-- ======================================================
-- 2. USUARIOS PARA EMPRESA_ID = 2 (MAQSA)
-- ======================================================
PRINT '>> Insertando usuario para Empresa 2...';

-- Edwin Roy Suárez Sánchez
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('42642076', 'Edwin Roy Suárez Sánchez', 'almacen@maqsa.pe', '983059270', 'password123', 1);
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (2, SCOPE_IDENTITY(), 4, 1);
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (3, SCOPE_IDENTITY(), 4, 1);

-- Magno Martínez
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('43115775', 'Socrates Magno Martinez Terrones', 'mmartinez@sblworldperu.com', '913097873', 'password123', 1); 
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (4, SCOPE_IDENTITY(), 2, 1);

-- Mario Sánchez
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('46643608', 'Mario Miguel Sanchez Vera', 'msanchez@sblworldperu.com', '986341713', 'password123', 1); 
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (4, SCOPE_IDENTITY(), 2, 1);

PRINT '>> Proceso de inserción finalizado correctamente.';
GO

PRINT '>> Listado de usuarios.';
SELECT 
    e.razon_social AS Empresa,
    e.ruc AS RUC,
    u.dni AS DNI,
    u.nombre AS Usuario,
    u.email AS Email,
    tu.nombre AS Rol,
    eu.estado AS RelacionActiva
FROM empresa_usuario eu
INNER JOIN empresa e ON eu.empresa_id = e.id
INNER JOIN usuario u ON eu.usuario_id = u.id
INNER JOIN tipo_usuario tu ON eu.tipo_usuario_id = tu.id
-- Opcional: Para ver solo los que están activos en la empresa
WHERE eu.estado = 1 AND e.estado = 1
ORDER BY e.razon_social, u.nombre;

GO