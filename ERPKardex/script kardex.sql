-- USE DB
use erp_kardex_dev;

drop trigger if exists trg_ActualizarDetraccionesUIT;
drop table if exists configuracion_general;
drop table if exists empresa_usuario_permiso;
drop table if exists permiso;
drop table if exists stock_almacen;
drop table if exists empresa;
drop table if exists sucursal;
drop table if exists almacen;
drop table if exists motivo;
drop table if exists tipo_cuenta;
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
drop table if exists personal;
drop table if exists activo_grupo;
drop table if exists activo_tipo;
drop table if exists activo;
drop table if exists activo_especificacion;
drop table if exists activo_documento;
drop table if exists activo_historial_medida;
drop table if exists movimiento_activo;
drop table if exists dmovimiento_activo;
drop table if exists banco;
drop table if exists tipo_cambio;
drop table if exists orden_pago;
drop table if exists tipo_documento_identidad;
drop table if exists tipo_persona;
drop table if exists pais;
drop table if exists ciudad;
drop table if exists origen;
drop table if exists proveedor;
drop table if exists cliente;
drop table if exists documento_pagar_aplicacion;
drop table if exists ddocumento_pagar;
drop table if exists documento_pagar;
drop table if exists tipo_detraccion;
drop table if exists detraccion;
drop table if exists cuenta_contable;

GO

CREATE TABLE tipo_documento_interno (
    id INT IDENTITY(1,1) PRIMARY KEY,
    codigo VARCHAR(20),      -- Ej: PED, PS, REQ, NI (Nota Ingreso)
    descripcion VARCHAR(200),
    ultimo_correlativo INT DEFAULT 0, -- Para llevar el control del número actual (ej. va en el 150)
    tipo_documento_id INT,
    estado BIT DEFAULT 1
);

CREATE TABLE tipo_usuario (
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(255),
    es_administrador BIT DEFAULT 0,
    estado BIT DEFAULT 1
);

-- Tabla de Usuarios (Globales, sin empresa_id aquí)
CREATE TABLE usuario (
    id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    dni CHAR(8) NOT NULL,
    nombre VARCHAR(255) NOT NULL,
    cargo VARCHAR(255),
    email VARCHAR(255),
    telefono VARCHAR(20),
    password VARCHAR(MAX) NOT NULL,
    estado BIT NOT NULL DEFAULT 1,
    fecha_registro DATETIME DEFAULT GETDATE()
);

-- Tabla Intermedia (Relación N a N Manual)
CREATE TABLE empresa_usuario (
    id INT IDENTITY(1,1) PRIMARY KEY,
    empresa_id INT NOT NULL,      -- Relación lógica con tabla empresa
    usuario_id INT NOT NULL,      -- Relación lógica con tabla usuario
    tipo_usuario_id INT NOT NULL, -- Relación lógica con tabla tipo_usuario
    estado BIT DEFAULT 1
);

-- 1. TABLA PERMISOS (El catálogo de qué se puede hacer)
CREATE TABLE permiso (
    id INT IDENTITY(1,1) PRIMARY KEY,
    -- Identificador único para el código C# (AuthorizePermiso)
    codigo VARCHAR(50) NOT NULL UNIQUE, 
    -- Texto visible para el usuario en la configuración
    descripcion VARCHAR(100) NOT NULL,
    -- Agrupación visual (Opcional si usas jerarquía, pero útil para iconos)
    modulo VARCHAR(50), 
    -- Jerarquía (Árbol)
    padre_id INT NULL, 
    -- Orden de visualización (1, 2, 3...)
    orden INT DEFAULT 0,
    estado BIT DEFAULT 1,
    -- FK recursiva (Un permiso apunta a otro permiso padre)
    CONSTRAINT FK_Permiso_Padre FOREIGN KEY (padre_id) REFERENCES permiso(id)
);

-- 2. TABLA ASIGNACIÓN (Quién tiene qué, en qué empresa)
CREATE TABLE empresa_usuario_permiso (
    id INT IDENTITY(1,1) PRIMARY KEY,
    empresa_usuario_id INT NOT NULL,     -- El ID del vínculo usuario-empresa
    permiso_id INT NOT NULL,
    
    CONSTRAINT FK_EUP_Vinculo FOREIGN KEY (empresa_usuario_id) REFERENCES empresa_usuario(id),
    CONSTRAINT FK_EUP_Permiso FOREIGN KEY (permiso_id) REFERENCES permiso(id),
    CONSTRAINT UQ_Permiso_Unico UNIQUE (empresa_usuario_id, permiso_id)
);

create table estado (
	id INT IDENTITY(1,1) PRIMARY KEY,
	nombre varchar(255),
	tabla varchar(255)
);

create table empresa (
	id INT IDENTITY(1,1) PRIMARY KEY,
	ruc char(11),
    nombre varchar(255),
	razon_social varchar(255),
    direccion varchar(255),
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
    simbolo varchar(255),
	estado BIT
);

create table motivo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	tipo_movimiento BIT, -- 1: INGRESO, 0: SALIDA
	descripcion VARCHAR(255),
	estado BIT
);

create table tipo_cuenta (
    id INT IDENTITY(1,1) PRIMARY KEY,
    codigo VARCHAR(255),
    nombre VARCHAR(255),
    numero_cuenta VARCHAR(255),
    estado BIT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

create table centro_costo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    codigo VARCHAR(20),
    nombre VARCHAR(255),
    empresa_id INT,
    padre_id INT,
    es_imputable BIT DEFAULT 1,
    fecha_inicio DATE,
    fecha_fin DATE,
    tipo_cuenta_id INT,
    cuenta_cargo VARCHAR(255),
    cuenta_abono VARCHAR(255),
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

create table origen (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(255),
    estado BIT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

CREATE TABLE tipo_documento_identidad (
    id INT IDENTITY(1,1) PRIMARY KEY,
    codigo VARCHAR(20),      -- 'RUC', 'DNI', 'CE', 'TAX', 'PAS'
    descripcion VARCHAR(100), -- 'Régimen Único de Contribuyentes', etc.
    longitud INT NULL,       -- Para validación (11, 8, etc.)
    es_alfanumerico BIT DEFAULT 0, -- 0: Solo números, 1: Letras y números
    estado BIT DEFAULT 1
);

create table tipo_persona (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(255),
    estado BIT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

create table pais (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre varchar(255),
    name varchar(255),
    nom varchar(255),
    iso2 varchar(255),
    iso3 varchar(255),
    phone_code varchar(255),
    continente varchar(255),
    estado BIT,
);

create table ciudad (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre varchar(255),
    estado BIT,
    pais_id INT,
);

CREATE TABLE proveedor (
	id INT IDENTITY(1,1) PRIMARY KEY,
    origen_id INT, -- NACIONAL O EXTRANJERO
    tipo_persona_id INT, -- PERSONA NATURAL, JURÍDICA, NO DOMICILIADO
    tipo_documento_identidad_id INT,
    numero_documento VARCHAR(50),
    razon_social varchar(255),
    direccion varchar(255),
    pais_id INT,
    ciudad_id INT,
	nombre_contacto varchar(255),
    cargo_contacto varchar(255),
    correo_electronico varchar(255),
    telefono varchar(255),
    banco_id INT,
    codigo_swift varchar(255),
    -- 1era cuenta
    moneda_id_uno INT,
    numero_cuenta_uno VARCHAR(255),
    numero_cci_uno varchar(255),
    -- 2da cuenta
    moneda_id_dos INT,
    numero_cuenta_dos VARCHAR(255),
    numero_cci_dos VARCHAR(255),
    -- 3era cuenta
    moneda_id_tres INT,
    numero_cuenta_tres VARCHAR(255),
    numero_cci_tres VARCHAR(255),
    -- ESTA ES DEL BANCO DE LA NACIÓN
    numero_cuenta_detracciones VARCHAR(255),
	estado BIT,
	empresa_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

CREATE TABLE cliente (
    id INT IDENTITY(1,1) PRIMARY KEY,
	origen_id INT, -- NACIONAL O EXTRANJERO
    tipo_persona_id INT, -- PERSONA NATURAL, JURÍDICA, NO DOMICILIADO
    tipo_documento_identidad_id INT,
    numero_documento VARCHAR(50),
    razon_social varchar(255),
    direccion varchar(255),
    pais_id INT,
    ciudad_id INT,
	nombre_contacto varchar(255),
    cargo_contacto varchar(255),
    correo_electronico varchar(255),
    telefono varchar(255),
    banco_id INT,
    codigo_swift varchar(255),
    -- 1era cuenta
    moneda_id_uno INT,
    numero_cuenta_uno VARCHAR(255),
    numero_cci_uno varchar(255),
    -- 2da cuenta
    moneda_id_dos INT,
    numero_cuenta_dos VARCHAR(255),
    numero_cci_dos VARCHAR(255),
    -- 3era cuenta
    moneda_id_tres INT,
    numero_cuenta_tres VARCHAR(255),
    numero_cci_tres VARCHAR(255),
	estado BIT,
	empresa_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
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
	moneda_id int,
    id_referencia INT NULL,
    tabla_referencia VARCHAR(50) NULL,
	estado_id int,
	usuario_id INT,
	fecha_registro DATETIME DEFAULT GETDATE(),
    usuario_anulacion_id INT,
    fecha_anulacion DATETIME,
	empresa_id INT,
	proveedor_id INT,
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
	precio decimal(19,10),
	igv decimal(19,10),
	subtotal decimal(19,10),
	total decimal(19,10),
	centro_costo_id int,
	actividad_id int,
    id_referencia INT NULL,
    tabla_referencia VARCHAR(50),
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
	empresa_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
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
    fecha_emision DATETIME,
    fecha_necesaria DATE,
    
    usuario_solicitante_id INT, -- Solo quién pide
    observacion VARCHAR(500),
    estado_id INT,              -- Solo: Pendiente, Aprobado, Rechazado
    
    empresa_id INT,

    -- APROBACIÓN
    usuario_aprobador INT,
    fecha_aprobacion DATETIME,
    usuario_registro INT,
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
    
    estado_id INT,
    lugar VARCHAR(255),
    empresa_id INT
);

-- 3.2 REQUERIMIENTO DE SERVICIO
CREATE TABLE reqservicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_interno_id INT, 
    numero VARCHAR(20),
    fecha_emision DATETIME,
    fecha_necesaria DATE,
    
    usuario_solicitante_id INT,
    observacion VARCHAR(500),
    estado_id INT,              -- Solo: Pendiente, Aprobado, Rechazado
    
    empresa_id INT,

    -- APROBACIÓN
    usuario_aprobador INT,
    fecha_aprobacion DATETIME,
    usuario_registro INT,
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
    
    estado_id INT,
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
    
    fecha_emision DATETIME,
    fecha_necesaria DATE,
    
    lugar_destino VARCHAR(255),
	sucursal_id INT,
	almacen_id INT,

    usuario_solicitante_id INT,    -- Quien procesa el pedido
    
    observacion VARCHAR(500),
    estado_id INT,
    
    empresa_id INT,
    usuario_registro INT,
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

    estado_id INT,
    empresa_id INT
);

-- 4.2 PEDIDO DE SERVICIO (PS) - ATIENDE RS
CREATE TABLE pedservicio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_interno_id INT, -- Referencia a 'PS'
    numero VARCHAR(20),            -- Ej: 'PS-00001'
    
    fecha_emision DATETIME,
    fecha_necesaria DATE,

	lugar_destino VARCHAR(255),
	sucursal_id INT,
	almacen_id INT,

    
    usuario_solicitante_id INT,
    observacion VARCHAR(500),
    estado_id INT,
    
    empresa_id INT,
    usuario_registro INT,
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
    estado_id INT,
    empresa_id INT
);

CREATE TABLE ordencompra (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_documento_interno_id INT, -- 'OCO'
    numero VARCHAR(20),            -- Ej: OCO-0000001
    
    -- Datos del Proveedor (Vital para la Orden)
    proveedor_id INT,                -- Proveedor seleccionado
    
    fecha_emision DATETIME,
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

    -- APROBACIÓN
    usuario_aprobador INT,
    fecha_aprobacion DATETIME,
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
    cantidad_atendida DECIMAL(12,2) DEFAULT 0,
    
    -- Valores Monetarios (Lo que negociaste con el proveedor)
    precio_unitario DECIMAL(19,10), -- NISIRA: P.Unitario
    porc_descuento DECIMAL(12,2) DEFAULT 0, -- NISIRA: %Dscto
    
    valor_venta DECIMAL(19,10),     -- Subtotal sin impuestos
    impuesto DECIMAL(19,10),        -- IGV del ítem
    total DECIMAL(19,10),           -- Total con impuestos
    
    centro_costo_id INT,           -- NISIRA: Destino/Centro de Costo
    lugar VARCHAR(255),
    -- TRAZABILIDAD (La clave para no "rayarse" con los pedidos)
    estado_id INT,
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
    
    proveedor_id INT,                -- Proveedor del Servicio
    
    fecha_emision DATETIME,
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

    -- APROBACIÓN
    usuario_aprobador INT,
    fecha_aprobacion DATETIME,
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
    cantidad_atendida DECIMAL(12,2) DEFAULT 0,
    precio_unitario DECIMAL(19,10),
    
    valor_venta DECIMAL(19,10),
    impuesto DECIMAL(19,10),
    total DECIMAL(19,10),
    
    centro_costo_id INT,
    lugar VARCHAR(255),

    -- TRAZABILIDAD
    estado_id INT,
    id_referencia INT,             -- ID de dpedservicio
    tabla_referencia VARCHAR(50) DEFAULT 'DPEDSERVICIO',
    
    empresa_id INT
);

-- Tabla para empleados/colaboradores que NO necesariamente acceden al sistema
CREATE TABLE personal (
    id INT IDENTITY(1,1) PRIMARY KEY,
    dni CHAR(8),
    nombres_completos VARCHAR(255),
    cargo VARCHAR(255),
    empresa_id INT, -- Se llenará en el siguiente paso
    estado BIT DEFAULT 1,
    fecha_registro DATETIME DEFAULT GETDATE()
);

CREATE TABLE activo_grupo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(100),
    estado BIT DEFAULT 1
);

CREATE TABLE activo_tipo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre VARCHAR(100),
    activo_grupo_id INT, -- Referencia lógica a activo_grupo(id)
    estado BIT DEFAULT 1
);

-- =================================================================================
-- 2. TABLA ACTIVO (Maestro de Vehículos y Equipos)
-- =================================================================================
CREATE TABLE activo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Identificación Visual
    codigo_interno VARCHAR(50), -- PLACA (Ej: M8J851)
    serie VARCHAR(100),         -- VIN / Chasis
    
    -- Clasificación
    activo_grupo_id INT, 
    activo_tipo_id INT,
    marca_id INT,  
    modelo_id INT, 
    
    -- Datos Operativos
    condicion VARCHAR(50), -- OPERATIVO, EN TALLER
    situacion VARCHAR(50), -- EN USO, EN STOCK
    
    -- Datos Vehiculares
    anio_fabricacion INT,
    color VARCHAR(50),
    modalidad_adquisicion VARCHAR(50), -- PROPIA, ALQUILADA
    
    -- [CORAZÓN DEL MANTENIMIENTO]
    medida_actual DECIMAL(10,2) DEFAULT 0,      -- Último KM registrado (se actualiza autom.)
    unidad_medida_uso VARCHAR(10) DEFAULT 'KM', 
    
    -- Alertas Gerenciales
    prox_mantenimiento DECIMAL(10,2) DEFAULT 0,  -- Ej: 52000
    frecuencia_mant DECIMAL(10,2) DEFAULT 5000,  -- Ej: Cada 5000
    
    -- Auditoría
    empresa_id INT, 
    sucursal_id INT,
    fecha_registro DATETIME DEFAULT GETDATE(),
    estado BIT DEFAULT 1
);

-- =================================================================================
-- 3. ESPECIFICACIONES (Motor, Combustible, GPS)
-- =================================================================================
CREATE TABLE activo_especificacion (
    id INT IDENTITY(1,1) PRIMARY KEY,
    activo_id INT NOT NULL, -- Referencia lógica a activo(id)
    clave VARCHAR(50),  
    valor VARCHAR(MAX)
);

-- =================================================================================
-- 4. DOCUMENTOS (SOAT, Rev Técnica) - Vital para Alertas
-- =================================================================================
CREATE TABLE activo_documento (
    id INT IDENTITY(1,1) PRIMARY KEY,
    activo_id INT NOT NULL, 
    
    tipo_documento VARCHAR(50), -- SOAT, REV_TECNICA
    nro_documento VARCHAR(100),
    fecha_emision DATE,
    fecha_vencimiento DATE,     -- Semáforo rojo si vence < 30 días
    
    aseguradora VARCHAR(100),
    ruta_archivo VARCHAR(500),
    estado BIT DEFAULT 1
);

CREATE TABLE activo_historial_medida (
    id INT IDENTITY(1,1) PRIMARY KEY,
    activo_id INT NOT NULL,
    
    fecha_lectura DATETIME DEFAULT GETDATE(),
    valor_medida DECIMAL(10,2), -- Ej: 45,200 KM
    
    origen_dato VARCHAR(50), -- 'ENTREGA', 'DEVOLUCION', 'CONTROL_SEMANAL', 'MANTENIMIENTO'
    observacion VARCHAR(255), -- Ej: "Revisión de lunes", "Carga de gasolina"
    
    usuario_registro_id INT, -- Quién tomó el dato
    estado BIT DEFAULT 1
);

-- =================================================================================
-- 5. MOVIMIENTOS (Cabecera del Acta)
-- =================================================================================
CREATE TABLE movimiento_activo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    codigo_acta VARCHAR(50),
    tipo_movimiento VARCHAR(20), -- ENTREGA, DEVOLUCION
    fecha_movimiento DATETIME DEFAULT GETDATE(),
    
    empresa_id INT,
    personal_id INT,         -- Responsable
    empresa_usuario_registro_id INT,
    usuario_registro_id INT, -- Usuario Logístico
    
    ubicacion_destino VARCHAR(255),
    observacion VARCHAR(500),
    ruta_acta_pdf VARCHAR(500),
    estado BIT DEFAULT 1
);

-- =================================================================================
-- 6. DETALLE MOVIMIENTO (Ítems)
-- =================================================================================
CREATE TABLE dmovimiento_activo (
    id INT IDENTITY(1,1) PRIMARY KEY,
    movimiento_activo_id INT NOT NULL, -- Referencia lógica a movimiento_activo(id)
    activo_id INT NOT NULL,            -- Referencia lógica a activo(id)
    
    condicion_item VARCHAR(50),
    
    -- Lectura del odómetro al momento del movimiento
    medida_registro DECIMAL(10,2) DEFAULT 0, 
    
    observacion_item VARCHAR(255)
);

CREATE TABLE tipo_cambio (
    id INT IDENTITY(1,1) PRIMARY KEY,
    fecha DATE NOT NULL,          -- La fecha del TC
    tc_compra DECIMAL(12,6),
    tc_venta DECIMAL(12,6),       -- ESTE ES EL QUE USAREMOS PARA VENTAS/SALIDAS
    estado BIT DEFAULT 1,
    fecha_registro DATETIME DEFAULT GETDATE(),
    
    -- Restricción: No puede haber dos registros para el '2025-01-14', por ejemplo.
    CONSTRAINT UQ_TipoCambio_Fecha UNIQUE (fecha)
);

CREATE TABLE orden_pago (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- VINCULACIÓN: Apuntamos a la DEUDA (Factura o Anticipo)
    documento_pagar_id INT NOT NULL, 
    
    -- DATOS DEL PAGO REAL
    numero VARCHAR(20),             -- Ej: OP-00001
    fecha_pago DATE NOT NULL,
    
    moneda_id INT NOT NULL,         
    tipo_cambio DECIMAL(12,6),      -- TC del día
    monto_pagado DECIMAL(18,2),     -- El monto de ESTA amortización
    
    -- TESORERÍA
    banco_id INT NULL,              
    numero_operacion VARCHAR(50),   -- Voucher / Cheque / Transferencia
    ruta_voucher VARCHAR(255),      
    
    -- ESTADOS (Generado / Anulado)
    estado_id INT,                  
    
    -- AUDITORÍA
    observacion VARCHAR(500),
    empresa_id INT,
    usuario_registro_id INT,
    fecha_registro DATETIME DEFAULT GETDATE(),
    usuario_anulacion_id INT,
    fecha_anulacion DATETIME
);

CREATE TABLE banco (
    id INT IDENTITY(1,1) PRIMARY KEY,
    ruc varchar(255),
    nombre VARCHAR(255),      -- Ej: 'BANCO DE CREDITO DEL PERU'
    estado BIT DEFAULT 1
);

-- ======================================================
-- 1. CABECERA: DOCUMENTO POR PAGAR
-- Centraliza Facturas, Boletas, RxH, Anticipos, Notas Crédito/Débito
-- ======================================================
CREATE TABLE documento_pagar (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- FILTROS PRINCIPALES
    empresa_id INT NOT NULL,
    proveedor_id INT NOT NULL,
    tipo_documento_interno_id INT NOT NULL, -- FAC, ANT, NC, ND

    -- EL AMARRE SAGRADO (1:1)
    -- Aunque en SQL permitimos NULL por flexibilidad técnica, 
    -- tu Lógica de Negocio OBLIGARÁ a que tengan datos.
    orden_compra_id INT NULL,      
    orden_servicio_id INT NULL,    
    
    -- REFERENCIA PARA NOTAS DE CRÉDITO/DÉBITO
    documento_referencia_id INT NULL, 

    -- DATOS DEL PAPEL FÍSICO
    serie VARCHAR(20),
    numero VARCHAR(50),
    fecha_emision DATETIME NOT NULL,
    fecha_vencimiento DATE,
    moneda_id INT,
    tipo_cambio DECIMAL(12,6),
    
    -- IMPORTES
    -- Subtotal, IGV, etc. para libros electrónicos
    subtotal DECIMAL(18,2) DEFAULT 0,
    monto_igv DECIMAL(18,2) DEFAULT 0,
    monto_inafecto DECIMAL(18,2) DEFAULT 0,
    
    total DECIMAL(18,2) DEFAULT 0, 
    
    -- EL SALDO VIVO
    -- Factura: Nace igual al Total. Baja cuando le aplicas un Anticipo o pagas.
    -- Anticipo: Nace igual al Total. Baja cuando lo usas para matar una Factura.
    saldo DECIMAL(18,2) DEFAULT 0, 
    monto_usado DECIMAL(18,2) DEFAULT 0,

    estado_id INT, -- Pendiente, Cancelado, Anulado
    observacion VARCHAR(500),
    usuario_registro_id INT,
    fecha_registro DATETIME DEFAULT GETDATE()
);

-- ======================================================
-- 2. DETALLE: ÍTEMS DEL DOCUMENTO
-- Solo para Facturas/Boletas que mueven inventario/gasto
-- ======================================================
CREATE TABLE ddocumento_pagar (
    id INT IDENTITY(1,1) PRIMARY KEY,
    documento_pagar_id INT NOT NULL,
    
    item VARCHAR(10), -- Correlativo visual '001', '002'
    
    -- TRAZABILIDAD EXACTA (Para saber qué ítem de la orden mataste)
    id_referencia INT NULL,      -- ID de dordencompra
    tabla_referencia VARCHAR(50), -- 'DORDENCOMPRA' o 'DORDENSERVICIO'
    
    -- DATOS (Copiados de la orden)
    producto_id INT,
    descripcion VARCHAR(MAX),
    unidad_medida VARCHAR(50),
    
    cantidad DECIMAL(12,2),      -- Cantidad facturada en ESTE documento
    precio_unitario DECIMAL(19,10),
    total DECIMAL(19,10)
);

-- ======================================================
-- 3. APLICACIONES (EL PANEL DE CRUCE)
-- Esta es la tabla que te faltaba para "amarrar" Anticipo con Factura
-- ======================================================
CREATE TABLE documento_pagar_aplicacion (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    empresa_id INT,
    
    -- EL DOCUMENTO QUE DEBE (LA FACTURA)
    documento_cargo_id INT NOT NULL, 
    
    -- EL DOCUMENTO QUE PAGA (EL ANTICIPO O NOTA DE CRÉDITO)
    documento_abono_id INT NOT NULL,
    
    monto_aplicado DECIMAL(18,2) NOT NULL, -- Cuánto del anticipo usé aquí
    
    fecha_aplicacion DATETIME DEFAULT GETDATE(),
    usuario_id INT
);

CREATE TABLE cuenta_contable (
    id INT IDENTITY(1,1) PRIMARY KEY,
    
    codigo VARCHAR(20) NOT NULL, -- Ej: '60', '601', '4212'
    nombre VARCHAR(255) NOT NULL,
    
    -- Relación lógica (Padre-Hijo)
    padre_id INT NULL, 
    nivel INT DEFAULT 1, -- 1: Elemento, 2: Cuenta, 3: Subcuenta...
    
    -- Clasificación para reportes automáticos
    tipo_elemento VARCHAR(20), -- 'ACTIVO', 'PASIVO', 'GASTO', 'INGRESO'
    
    es_movimiento BIT DEFAULT 1, -- 1: Se puede usar en facturas, 0: Es solo título
    
    empresa_id INT NULL, -- NULL si es plan estándar, ID si es específico
    estado BIT DEFAULT 1
);

create table tipo_detraccion (
    id INT IDENTITY(1,1) PRIMARY KEY,
    nombre varchar(255),
    estado BIT,
);

create table detraccion (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tipo_id INT,
    descripcion varchar(255),
    porcentaje decimal(5,2),
    importe_minimo decimal(18,2),
    porcentaje_uit decimal(5,2) null,
    estado bit default 1
);

-- 1. TABLA DE CONFIGURACIÓN (Siguiendo tu estilo)
CREATE TABLE configuracion_general (
    id INT IDENTITY(1,1) PRIMARY KEY,
    clave VARCHAR(50) UNIQUE NOT NULL, -- Ej: 'UIT_VALOR'
    valor VARCHAR(255) NOT NULL,
    descripcion VARCHAR(255),
    fecha_registro DATETIME DEFAULT GETDATE()
);

GO

-- 2. TRIGGER PARA BALANCEO DE CARGA
-- Automatiza el cálculo del importe_minimo basado en la UIT
CREATE TRIGGER trg_ActualizarDetraccionesUIT
ON configuracion_general
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- Si se actualiza el valor de la UIT
    IF EXISTS (SELECT 1 FROM inserted WHERE clave = 'UIT_VALOR')
    BEGIN
        DECLARE @ValorUIT DECIMAL(18,2);
        SELECT @ValorUIT = CAST(valor AS DECIMAL(18,2)) FROM inserted WHERE clave = 'UIT_VALOR';

        -- Actualizamos solo los que dependen de la UIT
        UPDATE detraccion
        SET importe_minimo = (@ValorUIT * porcentaje_uit / 100)
        WHERE porcentaje_uit IS NOT NULL 
          AND estado = 1;
    END
END;

GO

-- 1. Insertar Tipos de Detracción
INSERT INTO tipo_detraccion (nombre, estado) VALUES ('BIENES', 1);    -- Generará ID 1
INSERT INTO tipo_detraccion (nombre, estado) VALUES ('SERVICIOS', 1); -- Generará ID 2

-- 2. Insertar Valor base de la UIT (Usando 5500 para coincidir con tu tabla de ejemplo)
INSERT INTO configuracion_general (clave, valor, descripcion) 
VALUES ('UIT_VALOR', '5500', 'Valor de la UIT base para el cálculo de detracciones');

-- 3. Insertar Data Completa del Catálogo de Detracciones
-- Lógica: Si depende de la UIT, ponemos el % en porcentaje_uit. 
-- El Trigger calculará el importe_minimo automáticamente tras el insert o update de la UIT.

-- BIENES (ID 1)
INSERT INTO detraccion (tipo_id, descripcion, porcentaje, porcentaje_uit, importe_minimo) VALUES 
(1, 'AZÚCAR Y MELAZA DE CAÑA', 10.00, 50.00, 2750.00),
(1, 'ALCOHOL ETÍLICO', 10.00, 50.00, 2750.00),
(1, 'MINERALES DE ORO Y CONCENTRADOS (GRAVADOS CON IGV)', 10.00, 50.00, 2750.00),
(1, 'MINERALES METÁLICOS NO AURÍFEROS', 10.00, 50.00, 2750.00),
(1, 'RECURSOS HIDROBIOLÓGICOS', 4.00, NULL, 700.00),
(1, 'MAÍZ AMARILLO DURO', 4.00, NULL, 700.00),
(1, 'CAÑA DE AZÚCAR', 10.00, NULL, 700.00),
(1, 'ARENA Y PIEDRA', 10.00, NULL, 700.00),
(1, 'RESIDUOS, SUBPRODUCTOS, DESECHOS Y DESPERDICIOS', 15.00, NULL, 700.00),
(1, 'CARNES Y DESPOJOS COMESTIBLES', 4.00, NULL, 700.00),
(1, 'HARINA, POLVO Y PELLETS DE PESCADO', 4.00, NULL, 700.00),
(1, 'MADERA', 4.00, NULL, 700.00),
(1, 'ACEITE DE PESCADO', 10.00, NULL, 700.00),
(1, 'LECHE', 4.00, NULL, 700.00),
(1, 'PÁPRIKA Y FRUTOS CAPSICUM / PIMIENTA', 10.00, NULL, 700.00),
(1, 'PLOMO', 15.00, NULL, 700.00),
(1, 'BIENES GRAVADOS POR RENUNCIA A EXONERACIÓN IGV', 10.00, NULL, 700.00),
(1, 'BIENES EXONERADOS DEL IGV', 1.50, NULL, 700.00),
(1, 'ORO Y MINERALES METÁLICOS EXONERADOS DEL IGV', 1.50, NULL, 700.00),
(1, 'MINERALES NO METÁLICOS', 10.00, NULL, 700.00);

-- SERVICIOS (ID 2)
INSERT INTO detraccion (tipo_id, descripcion, porcentaje, porcentaje_uit, importe_minimo) VALUES 
(2, 'INTERMEDIACIÓN LABORAL Y TERCERIZACIÓN', 12.00, NULL, 700.00),
(2, 'ARRENDAMIENTO DE BIENES (MUEBLES E INMUEBLES)', 10.00, NULL, 700.00),
(2, 'MANTENIMIENTO Y REPARACIÓN DE BIENES MUEBLES', 12.00, NULL, 700.00),
(2, 'MOVIMIENTO DE CARGA', 10.00, NULL, 700.00),
(2, 'OTROS SERVICIOS EMPRESARIALES', 12.00, NULL, 700.00),
(2, 'COMISIÓN MERCANTIL', 10.00, NULL, 700.00),
(2, 'FABRICACIÓN DE BIENES POR ENCARGO', 10.00, NULL, 700.00),
(2, 'TRANSPORTE DE PERSONAS', 10.00, NULL, 700.00),
(2, 'CONTRATOS DE CONSTRUCCIÓN', 4.00, NULL, 700.00),
(2, 'DEMÁS SERVICIOS GRAVADOS CON IGV', 12.00, NULL, 700.00),
(2, 'TRANSPORTE DE BIENES POR VÍA TERRESTRE', 4.00, NULL, 400.00);

GO

-- ==========================================
-- 5. DATOS DE CONFIGURACIÓN INICIAL
-- ==========================================
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

GO

INSERT INTO tipo_documento_interno (codigo, descripcion, ultimo_correlativo) VALUES 
('IALM', 'NOTA DE INGRESO ALMACEN', 0),
('SALM', 'NOTA DE SALIDA ALMACEN', 0),
('REQ',  'REQUERIMIENTO DE COMPRA', 0),
('RS',   'REQUERIMIENTO DE SERVICIO', 0),
('PED',  'PEDIDO DE COMPRA', 0),
('PS',   'PEDIDO DE SERVICIO', 0),
('OCO',  'ORDEN DE COMPRA', 0),
('OS',  'ORDEN DE SERVICIO', 0);

-- =============================================================================
-- 3. TIPOS DE DOCUMENTO INTERNO (Para tu correlativo FAC-0001, etc.)
-- =============================================================================
-- Esto es vital para el 'switch' que haremos en el Controlador
INSERT INTO tipo_documento_interno (codigo, descripcion, ultimo_correlativo, tipo_documento_id) VALUES 
('FAC', 'FACTURA', 0, (select id from tipo_documento where codigo like '01')),
('BOL', 'BOLETA', 0, (select id from tipo_documento where codigo like '03')),
('RH',  'RECIBO POR HONORARIOS', 0, (select id from tipo_documento where codigo like '02')),
('NC',  'NOTA CRÉDITO', 0, (select id from tipo_documento where codigo like '07')),
('ND',  'NOTA DÉBITO', 0, (select id from tipo_documento where codigo like '08')),
('ANT', 'ANTICIPO', 0, NULL),
('LET', 'LETRA POR PAGAR', 0, NULL);

-- inserts de 'estado'
INSERT INTO estado (nombre, tabla) VALUES ('Aprobado', 'INGRESOSALIDAALM');
INSERT INTO estado (nombre, tabla) VALUES ('Anulado', 'INGRESOSALIDAALM');

-- Solo los estados que pediste para los REQUERIMIENTOS
INSERT INTO estado (nombre, tabla) VALUES 
('Pendiente', 'REQ'),
('Aprobado', 'REQ'),
('Atendido Parcial', 'REQ'),
('Atendido Total', 'REQ'),
('Rechazado', 'REQ');

-- Solo los estados que pediste para los DREQUERIMIENTOS
INSERT INTO estado (nombre, tabla) VALUES 
('Pendiente', 'DREQ'),
('Atendido', 'DREQ');

-- Solo los estados que pediste para los DPEDIDOS
INSERT INTO estado (nombre, tabla) VALUES 
('Pendiente', 'DPED'),
('Atendido Parcial', 'DPED'),
('Atendido Total', 'DPED');

-- Estados para el Pedido (Operativos)
INSERT INTO estado (nombre, tabla) VALUES 
('Generado', 'PED'),
('Atendido Parcial', 'PED'),
('Atendido Total', 'PED');

-- ORDEN DE COMPRA/SERVICIO
INSERT INTO estado (nombre, tabla) VALUES ('Generado', 'ORDEN');
INSERT INTO estado (nombre, tabla) VALUES ('Anulado', 'ORDEN');
INSERT INTO estado (nombre, tabla) VALUES ('Aprobado', 'ORDEN');

-- ORDEN DE COMPRA Y ORDEN DE SERVICIO
INSERT INTO estado (nombre, tabla) VALUES ('Pendiente Pago', 'ORDEN'); -- ID X
INSERT INTO estado (nombre, tabla) VALUES ('Pagado Parcial', 'ORDEN'); -- ID Y
INSERT INTO estado (nombre, tabla) VALUES ('Pagado Total', 'ORDEN');   -- ID Z
INSERT INTO estado (nombre, tabla) VALUES ('Vencido', 'ORDEN');        -- ID W

-- Solo los estados que pediste para los DORDEN
INSERT INTO estado (nombre, tabla) VALUES 
('Pendiente', 'DORDEN'),
('Atendido Parcial', 'DORDEN'),
('Atendido Total', 'DORDEN');

-- =============================================================================
-- 2. ESTADOS PARA LA DEUDA (El ciclo de vida del dinero)
-- =============================================================================
-- Tabla: 'PAGO' (Finanzas General)
INSERT INTO estado (nombre, tabla) VALUES ('Pagado', 'ORDEN_PAGO');
INSERT INTO estado (nombre, tabla) VALUES ('Anulado', 'ORDEN_PAGO');

INSERT INTO estado (nombre, tabla) VALUES ('Por Pagar', 'DOCUMENTO_PAGAR');      -- Deuda viva (Factura o Anticipo sin depositar)
INSERT INTO estado (nombre, tabla) VALUES ('Cancelado', 'DOCUMENTO_PAGAR'); -- Deuda pagada (Factura cerrada)
INSERT INTO estado (nombre, tabla) VALUES ('Disponible', 'DOCUMENTO_PAGAR'); -- Deuda pagada (Anticipo listo para usar)
INSERT INTO estado (nombre, tabla) VALUES ('Agotado', 'DOCUMENTO_PAGAR'); -- Anticipo consumido al 100% en facturas
INSERT INTO estado (nombre, tabla) VALUES ('Anulado', 'DOCUMENTO_PAGAR');

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
INSERT INTO empresa (ruc, razon_social, nombre, estado) VALUES ('20607778338', 'CONTROL SCIENCE DEL PERU S.A.C.', 'CONTROL SCIENCE', 1);
INSERT INTO empresa (ruc, razon_social, nombre, estado) VALUES ('20603727551', 'STALNO S.A.C.', 'STALNO', 1);
INSERT INTO empresa (ruc, razon_social, nombre, estado) VALUES ('20613898167', 'MAQUINARIA Y SANIDAD AGRICOLA S.A.C.', 'MAQSA', 1);
INSERT INTO empresa (ruc, razon_social, nombre, estado) VALUES ('20615184153', 'SUPPLY BIOTECHNOLOGY LOGISTIC WORLD S.A.C.S.', 'SUPPLY BIOTECHNOLOGY', 1);

GO
-- B. INSERTAR LAS DEMÁS EMPRESAS (IDs 5 al 16)
-- ------------------------------------------------------------------
SET IDENTITY_INSERT empresa ON;
-- ID 5: SOLUCIONES INDUSTRIALES METQUIM
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 5)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (5, '20614551853', 'SOLUCIONES INDUSTRIALES METQUIM S.A.C.', 'METQUIM', 1);

-- ID 6: GREEN FARM
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 6)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (6, '20603845294', 'PRODUCTOS Y SERVICIOS GENERALES GREEN FARM S.A.C.', 'GREEN FARM', 1);

-- ID 7: IMBO
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 7)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (7, '20605644725', 'CONSTRUCTORA INMOBILIARIA EDIFICACIONES E INGENIERIA IMBO S.A.C', 'IMBO', 1);

-- ID 8: EVOCA
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 8)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (8, '20561231304', 'EVOCA S.A.C.', 'EVOCA', 1);

-- ID 9: INSTITUTO E.I.R.L.
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 9)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (9, '20605353721', 'INSTITUTO DE INVESTIGACION E INNOVACION DE GESTION Y DESARROLLO EMPRESARIAL E.I.R.L.', 'INIGDE', 1);

-- ID 10: MEDICINA CORPORATIVA
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 10)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (10, '20607165832', 'MEDICINA CORPORATIVA Y SALUD S.A.C.', 'MEDICORS', 1);

-- ID 11: COMERCIALIZADORA EXPORTADORA
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 11)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (11, '20609093561', 'COMERCIALIZADORA EXPORTADORA Y DISTRIBUIDORA S.A.C.', 'COMEXDI', 1);

-- ID 12: AGROQUIMEX
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 12)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (12, '20612680842', 'INNOVACION Y GESTION EN BIOPLAGUICIDAS AGROQUIMEX S.A.C', 'AGROQUIMEX', 1);

-- ID 13: ECOMATERIALES
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 13)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (13, '20615085198', 'CORPORACION ECOMATERIALES DEL PERU S.A.C.', 'ECOMAT', 1);

-- ID 14: RECLUTA HEAD HUNTING
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 14)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (14, '20615125254', 'RECLUTA HEAD HUNTING S.A.C.S', 'RECLUTA', 1);

-- ID 15: YNNOVA DIGITAL
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 15)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (15, '20615155251', 'YNNOVA DIGITAL CORP S.A.C.S.', 'YNNOVA', 1);

-- ID 16: TRUST MORE COMPLIANCE
IF NOT EXISTS(SELECT * FROM empresa WHERE id = 16)
    INSERT INTO empresa (id, ruc, razon_social, nombre, estado) VALUES (16, '20614993198', 'TRUST MORE COMPLIANCE S.A.C.S.', 'TRUST MORE COMPLIANCE', 1);

SET IDENTITY_INSERT empresa OFF;
GO

-- inserts de 'sucursal'
INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('001', 'PRINCIPAL - POMALCA', 1, 1);

-- inserts de 'almacen'
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, sucursal_id, es_valorizado, empresa_id) VALUES ('01','PRINCIPAL',1,'001', 1, 1, 1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, sucursal_id, es_valorizado, empresa_id) VALUES ('02','TERCEROS',1,'001', 1, 1, 1);

-- inserts de 'moneda'
INSERT INTO moneda (codigo, nombre, simbolo, estado) VALUES ('01', 'SOLES', 'S/.', 1);
INSERT INTO moneda (codigo, nombre, simbolo, estado) VALUES ('02', 'DÓLARES', '$', 1);

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

---- inserts de 'centro_costo'
---- NIVEL 1: PADRES RAÍZ
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
--('C1101', 'TERRENOS', 1, NULL, 0, 1),
--('C1102', 'ACTIVO FIJO', 1, NULL, 0, 1),
--('C1103', 'GESTION ADMINISTRATIVA', 1, NULL, 0, 1),
--('C1104', 'GESTION OPERATIVA', 1, NULL, 0, 1),
--('C1105', 'GESTION VENTAS', 1, NULL, 0, 1);

---- NIVEL 2: HIJOS DIRECTOS
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
--('C110101', 'TERRENO PROPIO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1101'), 1, 1),
--('C110102', 'TERRENO ALQUILADO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1101'), 0, 1),
--('C110201', 'INFRAESTRUCTURA', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
--('C110202', 'MAQUINARIA Y EQUIPOS', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
--('C110203', 'EQUIPOS AUXILIARES', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
--('C110204', 'VEHICULOS', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
--('C110205', 'INTANGIBLES', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1102'), 1, 1),
--('C110301', 'GERENCIA GENERAL', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1103'), 1, 1),
--('C110401', 'PROCESO PRODUCTIVO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
--('C110402', 'COMPRA DE INSUMOS', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
--('C110404', 'INVESTIGACION Y DESARROLLO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
--('C110405', 'CONTROL DE CALIDAD', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
--('C110406', 'MANTENIMIENTO', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1104'), 1, 1),
--('C110501', 'VENTAS NACIONALES (PERU)', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C1105'), 0, 1);

---- NIVEL 3: NIETOS
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
--('C11010201', 'ALMACEN POMALCA', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110102'), 1, 1),
--('C11050101', 'VENTA INSUMOS', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110501'), 1, 1),
--('C11050102', 'SOPORTE POST-VENTA', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110501'), 1, 1),
--('C11050103', 'ENSAYOS Y DEMOSTRACIONES', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110501'), 1, 1),
--('C11050104', 'GESTION COMERCIAL - PERU', 1, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C110501'), 1, 1);

---- NIVEL 1: PADRES RAÍZ (empresa_id = 2)
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
--('C0101', 'TERRENOS', 2, NULL, 0, 1),
--('C0102', 'ACTIVO FIJO', 2, NULL, 0, 1),
--('C0103', 'GESTION ADMINISTRATIVA', 2, NULL, 0, 1),
--('C0104', 'GESTION OPERACIONES', 2, NULL, 0, 1),
--('C0105', 'GESTION VENTAS', 2, NULL, 0, 1);

---- NIVEL 2: HIJOS DIRECTOS
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
--('C010101', 'TERRENO PROPIO', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0101' AND empresa_id = 2), 1, 1),
--('C010102', 'TERRENO ALQUILADO', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0101' AND empresa_id = 2), 1, 1),
--('C010201', 'INFRAESTRUCTURA', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0102' AND empresa_id = 2), 1, 1),
--('C010202', 'MAQUINARIA Y EQUIPOS', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0102' AND empresa_id = 2), 1, 1),
--('C010203', 'EQUIPOS AUXILIARES', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0102' AND empresa_id = 2), 1, 1),
--('C010204', 'VEHICULOS', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0102' AND empresa_id = 2), 0, 1),
--('C010301', 'OFICINA DMINISTRATIVA', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0103' AND empresa_id = 2), 1, 1),
--('C010302', 'GERENTE GENERAL', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0103' AND empresa_id = 2), 1, 1),
--('C010401', 'SOLUCIONES INDUSTRIALES', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0104' AND empresa_id = 2), 1, 1),
--('C010402', 'COMERCIALIZACION DE MATERIALES', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0104' AND empresa_id = 2), 1, 1),
--('C010501', 'VENTAS Y COTIZACIONES', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1),
--('C010502', 'MARKETING DIGITAL', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1),
--('C010503', 'ATENCION AL CLIENTE', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1),
--('C010504', 'POSTVENTA Y GARANTIAS', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1),
--('C010505', 'GESTION COMERCIAL', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0105' AND empresa_id = 2), 1, 1);

---- NIVEL 3: NIETOS (Placas de vehículos)
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
--('C01020401', 'FORD RANGER - PLACA M8J851', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C010204' AND empresa_id = 2), 1, 1),
--('C01020402', 'RENAULT OROCH - PLACA M8K701', 2, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C010204' AND empresa_id = 2), 1, 1);

---- NIVEL 1: PADRES RAÍZ (empresa_id = 3)
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
--('C0301', 'TERRENOS', 3, NULL, 0, 1),
--('C0302', 'ACTIVO FIJO', 3, NULL, 0, 1),
--('C0303', 'GESTION ADMINISTRATIVA', 3, NULL, 0, 1),
--('C0304', 'GESTION OPERACIONES', 3, NULL, 0, 1),
--('C0305', 'GESTION VENTAS', 3, NULL, 0, 1);

---- NIVEL 2: HIJOS DIRECTOS
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
--('C030101', 'TERRENO PROPIO', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0301' AND empresa_id = 3), 1, 1),
--('C030102', 'TERRENO ALQUILADO', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0301' AND empresa_id = 3), 1, 1),
--('C030201', 'INFRAESTRUCTURA', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0302' AND empresa_id = 3), 1, 1),
--('C030202', 'MAQUINARIA Y EQUIPOS', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0302' AND empresa_id = 3), 1, 1),
--('C030203', 'EQUIPOS AUXILIARES', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0302' AND empresa_id = 3), 1, 1),
--('C030204', 'VEHICULOS', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0302' AND empresa_id = 3), 0, 1),
--('C030301', 'OFICINA DMINISTRATIVA', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0303' AND empresa_id = 3), 1, 1),
--('C030302', 'GERENTE GENERAL', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0303' AND empresa_id = 3), 1, 1),
--('C030401', 'SERVICIOS DE MANTENIMIENTO', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0304' AND empresa_id = 3), 1, 1),
--('C030402', 'COMERCIALIZACION DE MATERIALES', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0304' AND empresa_id = 3), 1, 1),
--('C030501', 'VENTAS Y COTIZACIONES', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1),
--('C030502', 'MARKETING DIGITAL', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1),
--('C030503', 'ATENCION AL CLIENTE', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1),
--('C030504', 'POSTVENTA Y GARANTIAS', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1),
--('C030505', 'GESTION COMERCIAL', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C0305' AND empresa_id = 3), 1, 1);

---- NIVEL 3: NIETOS
--INSERT INTO centro_costo (codigo, nombre, empresa_id, padre_id, es_imputable, estado) VALUES
--('C03020401', 'FORD RANGER XLS - SIN PLACA', 3, (SELECT TOP 1 id FROM centro_costo WHERE codigo = 'C030204' AND empresa_id = 3), 1, 1);

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
INSERT INTO tipo_usuario (nombre, estado, es_administrador) VALUES ('ADMINISTRADOR DEL SISTEMA', 1, 1); -- ID 1
INSERT INTO tipo_usuario (nombre, estado, es_administrador) VALUES ('USUARIO', 1, 0);       -- ID 4

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
VALUES (1, @NewUsuarioID, 1, 1); -- Empresa 1, Rol 1 (Administrador del sistema)

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
VALUES (1, SCOPE_IDENTITY(), 2, 1);

-- Lilyan Lozada Diaz
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('73138239', 'Lilyan Lozada Diaz', 'llozada@agrosayans.com', '930939954', 'password123', 1);
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (1, SCOPE_IDENTITY(), 2, 1);

-- Katherin Espinal Vasquez
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('75185380', 'Katherin Espinal Vasquez', 'kespinal@agrosayans.com', '977796697', 'password123', 1);
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (1, SCOPE_IDENTITY(), 2, 1);

-- ======================================================
-- 2. USUARIOS PARA EMPRESA_ID = 2 (MAQSA)
-- ======================================================
PRINT '>> Insertando usuario para Empresa 2...';

-- Edwin Roy Suárez Sánchez
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('42642076', 'Edwin Roy Suárez Sánchez', 'almacen@maqsa.pe', '983059270', 'password123', 1);
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (2, SCOPE_IDENTITY(), 2, 1);
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (3, SCOPE_IDENTITY(), 2, 1);

-- Magno Martínez
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('43115775', 'Socrates Magno Martinez Terrones', 'mmartinez@sblworldperu.com', '913097873', 'password123', 1); 
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (4, SCOPE_IDENTITY(), 1, 1);

-- Mario Sánchez
INSERT INTO usuario (dni, nombre, email, telefono, password, estado)
VALUES ('46643608', 'Mario Miguel Sanchez Vera', 'msanchez@sblworldperu.com', '986341713', 'password123', 1); 
INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (4, SCOPE_IDENTITY(), 2, 1);

PRINT '>> Proceso de inserción finalizado correctamente.';
GO

INSERT INTO usuario (dni, nombre, email, telefono, password, estado) 
VALUES ('77013712', 'Fernando Dávila Ubillús', 'ssoporte@corpsaf.com', '913980405', 'password123', 1);

INSERT INTO empresa_usuario (empresa_id, usuario_id, tipo_usuario_id, estado)
VALUES (15, (select id from usuario where dni = '77013712'), 1, 1);

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

INSERT INTO banco (ruc, nombre, estado) VALUES ('20100047218','BANCO DE CRÉDITO DEL PERÚ (BCP)',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20100130204','BBVA PERÚ',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20100053455','INTERBANK (BANCO INTERNACIONAL DEL PERÚ)',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20100030595','BANCO DE LA NACIÓN',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20101036813','BANBIF (BANCO INTERAMERICANO DE FINANZAS)',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20330401991','BANCO FALABELLA PERÚ',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20259702411','BANCO RIPLEY PERÚ',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20516711559','BANCO SANTANDER PERÚ',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20100105862','BANCO PICHINCHA',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20513074370','BANCO GNB PERÚ',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20604306150','BANK OF CHINA (PERÚ)',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20100116635','CITIBANK DEL PERÚ',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20517476405','ALFIN BANCO',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20369155360','COMPARTAMOS BANCO',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20382036655','MIBANCO – BANCO DE LA MICROEMPRESA',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20546892175','ICBC PERU BANK S.A.',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20100209641','CAJA MUNICIPAL DE AREQUIPA',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20114839176','CAJA MUNICIPAL DE CUSCO',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20130200789','CAJA MUNICIPAL DE HUANCAYO',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20104888934','CAJA MUNICIPAL DE ICA',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20100269466','CAJA METROPOLITANA DE LIMA',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20103845328','CAJA MUNICIPAL DE MAYNAS',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20102361939','CAJA MUNICIPAL DE PAITA',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20113604248','CAJA MUNICIPAL DE PIURA',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20130098488','CAJA MUNICIPAL DE TACNA',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20132243230','CAJA MUNICIPAL DE TRUJILLO',1);
INSERT INTO banco (ruc, nombre, estado) VALUES ('20114105024','CAJA MUNICIPAL DEL SANTA',1);

GO

INSERT INTO tipo_documento_identidad (codigo, descripcion, longitud, es_alfanumerico) VALUES 
('RUC', 'RUC', 11, 0),
('DNI', 'DNI', 8, 0),
('TAX', 'TAX ID', 20, 1);

GO

INSERT INTO origen (nombre, estado) VALUES
('NACIONAL', 1),
('EXTRANJERO', 1);

GO

INSERT INTO tipo_persona (nombre, estado) VALUES
('PERSONA NATURAL', 1),
('PERSONA JURÍDICA', 1),
('NO DOMICILIADO', 1);

GO

INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-01',3.358,3.368,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-02',3.358,3.368,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-03',3.358,3.368,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-04',3.358,3.368,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-05',3.358,3.368,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-06',3.356,3.372,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-07',3.357,3.366,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-08',3.359,3.368,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-09',3.359,3.368,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-10',3.358,3.365,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-11',3.358,3.365,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-12',3.358,3.365,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-13',3.355,3.368,1);
INSERT INTO tipo_cambio (fecha, tc_compra, tc_venta, estado) VALUES ('2026-01-14',3.356,3.361,1);

GO

-- 1. NODOS RAÍZ (Las Cabeceras o Menús Principales)
INSERT INTO permiso (codigo, descripcion, modulo, padre_id, orden) VALUES 
('MOD_MAESTROS', 'Módulo Registros Maestros', 'MAESTROS', NULL, 1),
('MOD_ALMACEN', 'Módulo Control Almacén', 'ALMACEN', NULL, 2),
('MOD_LOGISTICA', 'Módulo Logística Operativa', 'LOGISTICA', NULL, 3),
('MOD_FINANZAS', 'Módulo Gestión Financiera', 'FINANZAS', NULL, 4),
('MOD_ACTIVOS', 'Módulo Activos Fijos', 'ACTIVOS', NULL, 5),
('MOD_SEGURIDAD', 'Módulo de Seguridad', 'SEGURIDAD', NULL, 6);

-- Obtenemos los IDs generados para asignar los hijos
DECLARE @ID_MAE INT = (SELECT id FROM permiso WHERE codigo = 'MOD_MAESTROS');
DECLARE @ID_ALM INT = (SELECT id FROM permiso WHERE codigo = 'MOD_ALMACEN');
DECLARE @ID_LOG INT = (SELECT id FROM permiso WHERE codigo = 'MOD_LOGISTICA');
DECLARE @ID_FIN INT = (SELECT id FROM permiso WHERE codigo = 'MOD_FINANZAS');
DECLARE @ID_ACT INT = (SELECT id FROM permiso WHERE codigo = 'MOD_ACTIVOS');

-- 2. SUB-MENÚS (Maestros)
INSERT INTO permiso (codigo, descripcion, modulo, padre_id, orden) VALUES 
('MAE_PROD_REG', 'Registrar Producto', 'MAESTROS', @ID_MAE, 1),
('MAE_SERV_REG', 'Registrar Servicio', 'MAESTROS', @ID_MAE, 2),
('MAE_PROV_VER', 'Ver Proveedores', 'MAESTROS', @ID_MAE, 3),
('MAE_CLIE_VER', 'Ver Clientes', 'MAESTROS', @ID_MAE, 4);

-- 3. SUB-MENÚS (Almacén)
INSERT INTO permiso (codigo, descripcion, modulo, padre_id, orden) VALUES 
('ALM_STOCK_VER', 'Ver Reporte Stock', 'ALMACEN', @ID_ALM, 1),
('ALM_MOV_VER', 'Ver Ingresos/Salidas', 'ALMACEN', @ID_ALM, 2),
('ALM_KARDEX_VER', 'Ver Reporte Kardex', 'ALMACEN', @ID_ALM, 3);

-- 4. SUB-MENÚS (Logística)
INSERT INTO permiso (codigo, descripcion, modulo, padre_id, orden) VALUES 
('LOG_DASH_VER', 'Ver Dashboard Logístico', 'LOGISTICA', @ID_LOG, 0), -- Nivel superior
('LOG_REQ_COMPRA_VER', 'Ver Req. Compra', 'LOGISTICA', @ID_LOG, 1),
('LOG_REQ_SERV_VER', 'Ver Req. Servicio', 'LOGISTICA', @ID_LOG, 2),
-- Grupo Proceso de Compras
('LOG_PED_COMPRA_VER', 'Ver Pedidos Compra', 'LOGISTICA', @ID_LOG, 3),
('LOG_PED_SERV_VER', 'Ver Pedidos Servicio', 'LOGISTICA', @ID_LOG, 4),
('LOG_ORD_COMPRA_VER', 'Ver Órdenes Compra', 'LOGISTICA', @ID_LOG, 5),
('LOG_ORD_SERV_VER', 'Ver Órdenes Servicio', 'LOGISTICA', @ID_LOG, 6);

-- 5. SUB-MENÚS (Finanzas)
INSERT INTO permiso (codigo, descripcion, modulo, padre_id, orden) VALUES 
('FIN_CC_VER', 'Ver Centros de Costo', 'FINANZAS', @ID_FIN, 1),
('FIN_PAGO_VER', 'Gestión de Pagos', 'FINANZAS', @ID_FIN, 2),
('FIN_TC_VER', 'Ver Tipo de Cambio', 'FINANZAS', @ID_FIN, 3);

-- 6. SUB-MENÚS (Activos)
INSERT INTO permiso (codigo, descripcion, modulo, padre_id, orden) VALUES 
('ACT_DASH_VER', 'Dashboard Activos', 'ACTIVOS', @ID_ACT, 1),
('ACT_COMPUTO_VER', 'Activos Cómputo', 'ACTIVOS', @ID_ACT, 2),
('ACT_FLOTA_VER', 'Flota Vehicular', 'ACTIVOS', @ID_ACT, 3);

-- 7. ACCIONES ESPECÍFICAS (Botones de "Ojito", Aprobación, etc.)
-- Estos no salen en el menú, pero se configuran en el árbol
DECLARE @ID_OC INT = (SELECT id FROM permiso WHERE codigo = 'LOG_ORD_COMPRA_VER');

INSERT INTO permiso (codigo, descripcion, modulo, padre_id, orden) VALUES 
('BTN_OC_VER_DETALLE', 'Botón: Ver Detalle (Ojito)', 'LOGISTICA', @ID_OC, 1),
('BTN_OC_APROBAR', 'Botón: Aprobar Orden', 'LOGISTICA', @ID_OC, 2),
('BTN_OC_ANULAR', 'Botón: Anular Orden', 'LOGISTICA', @ID_OC, 3);
GO