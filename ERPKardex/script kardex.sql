-- USE DB
use erp_kardex;

drop table if exists empresa;
drop table if exists sucursal;
drop table if exists almacen;
drop table if exists motivo;
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
	cod_sucursal varchar(255),
	empresa_id int
);

create table moneda (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	nombre varchar(255),
	estado BIT
);

create table motivo (
	codigo varchar(255) PRIMARY KEY,
	tipo_movimiento BIT, -- 1: INGRESO, 0: SALIDA
	descripcion VARCHAR(255),
	estado BIT
);

create table tipo_documento (
	id INT IDENTITY(1,1) PRIMARY KEY,
	codigo varchar(255),
	descripcion varchar(max),
	estado BIT
);

create table ingresosalidaalm (
	id INT IDENTITY(1,1) PRIMARY KEY,
	fecha DATE,
	numero varchar(255),
	sucursal_id INT,
	almacen_id INT,
	cod_motivo varchar(255),
	fecha_documento DATE,
	tipo_documento_id varchar(255),
	serie_documento varchar(255),
	numero_documento varchar(255),
	moneda_id int,
	estado_id int,
	usuario_id INT,
	fecha_registro DATETIME DEFAULT GETDATE()
);

create table dingresosalidaalm (
	id INT IDENTITY(1,1) PRIMARY KEY,
	ingresosalidaalm_id INT,
	item varchar(255),
	cod_producto varchar(255),
	descripcion_producto varchar(255),
	cod_unidad_medida varchar(255),
	cantidad decimal(12,2),
	tipo_documento_id int,
	serie_documento varchar(255),
	numero_documento varchar(255),
	moneda_id int,
	tipo_cambio decimal(12,6),
	precio decimal(19,6),
	fecha_documento DATE,
	usuario_id INT,
	fecha_registro DATETIME DEFAULT GETDATE()
);

create table cuenta (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200)
);

create table grupo (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200),
	cuenta_id varchar(255)
);

create table subgrupo (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200),
	cod_grupo varchar(255),
	descripcion_grupo varchar(255),
	observacion varchar(255)
);

create table unidad_medida (
	codigo varchar(255) PRIMARY KEY,
	descripcion varchar(200)
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
	estado BIT
);

create table modelo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	nombre varchar(255),
	estado BIT,
	marca_id INT
);

create table producto (
	codigo varchar(255) PRIMARY KEY,
	cod_grupo varchar(255),
	descripcion_grupo varchar(255),
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
	cantidad decimal(12,2),
	estado BIT, -- 1: activo 0: inactivo
	empresa_id INT
);

create table ingrediente_activo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	descripcion varchar(255)
);

create table detalle_ingrediente_activo (
	id INT IDENTITY(1,1) PRIMARY KEY,
	cod_producto varchar(255),
	ingrediente_activo_id int,
	porcentaje decimal(12,2)
);

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
INSERT INTO cuenta (codigo, descripcion) VALUES ('21', 'MERCADERÍAS');
INSERT INTO cuenta (codigo, descripcion) VALUES ('24', 'MATERIALES SUMINISTROS Y REPUESTOS');
INSERT INTO cuenta (codigo, descripcion) VALUES ('33', 'ACTIVOS');
INSERT INTO cuenta (codigo, descripcion) VALUES ('63', 'SERVICIOS');

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
INSERT INTO empresa (ruc, razon_social, estado) VALUES ('20603727551', 'STALNO S.A.C.', 1);

-- inserts de 'sucursal'
INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('001', 'PRINCIPAL - CHICLAYO', 1, 1);
INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('002', 'SUCURSAL - MORROPE', 1, 1);
INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('002', 'PRINCIPAL - CHICLAYO', 1, 2);
INSERT INTO sucursal (codigo, nombre, estado, empresa_id) VALUES ('002', 'SUCURSAL - MORROPE', 1, 2);

-- inserts de 'almacen'
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('01','PRINCIPAL',1,'001',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('02','PRODUCTO TERMIANDO',1,'001',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('03','MERMAS Y DESPERDICIOS',1,'001',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('04','ENVASES Y EMBALAJES',1,'001',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('05','MATERIALES Y AUXILIARES',1,'001',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('01','PRINCIPAL',1,'002',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('02','PRODUCTO TERMIANDO',1,'002',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('03','MERMAS Y DESPERDICIOS',1,'002',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('04','ENVASES Y EMBALAJES',1,'002',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('05','MATERIALES Y AUXILIARES',1,'002',1);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('01','PRINCIPAL',1,'001',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('02','MERCADERIAS',1,'001',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('03','REPUESTOS',1,'001',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('04','SISTEMA DE RIEGO',1,'001',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('05','MAQUINARIA',1,'001',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('06','MATERIALES DE CONSTRUCCION',1,'001',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('07','EQUIPOS DE PROTECCION',1,'001',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('01','PRINCIPAL',1,'002',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('02','MERCADERIAS',1,'002',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('03','REPUESTOS',1,'002',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('04','SISTEMA DE RIEGO',1,'002',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('05','MAQUINARIA',1,'002',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('06','MATERIALES DE CONSTRUCCION',1,'002',2);
INSERT INTO almacen (codigo, nombre, estado, cod_sucursal, empresa_id) VALUES ('07','EQUIPOS DE PROTECCION',1,'002',2);

-- inserts de 'tipo_documento'
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('00','Otros',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('01','Factura',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('02','Recibo por Honorarios',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('03','Boleta de Venta',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('04','Liquidación de compra',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('05','Boleto de compañía de aviación comercial por el servicio de transporte aéreo de pasajeros',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('06','Carta de porte aéreo por el servicio de transporte de carga aérea',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('07','Nota de crédito',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('08','Nota de débito',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('09','Guía de remisión - Remitente',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('10','Recibo por Arrendamiento',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('11','Póliza emitida por las Bolsas de Valores, Bolsas de Productos o Agentes de Intermediación por operaciones realizadas en las Bolsas de Valores o Productos o fuera de las mismas, autorizadas por CONASEV',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('12','Ticket o cinta emitido por máquina registradora',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('13','Documento emitido por bancos, instituciones financieras, crediticias y de seguros que se encuentren bajo el control de la Superintendencia de Banca y Seguros',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('14','Recibo por servicios públicos de suministro de energía eléctrica, agua, teléfono, telex y telegráficos y otros servicios complementarios que se incluyan en el recibo de servicio público',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('15','Boleto emitido por las empresas de transporte público urbano de pasajeros',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('16','Boleto de viaje emitido por las empresas de transporte público interprovincial de pasajeros dentro del país',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('17','Documento emitido por la Iglesia Católica por el arrendamiento de bienes inmuebles',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('18','Documento emitido por las Administradoras Privadas de Fondo de Pensiones que se encuentran bajo la supervisión de la Superintendencia de Administradoras Privadas de Fondos de Pensiones',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('19','Boleto o entrada por atracciones y espectáculos públicos',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('20','Comprobante de Retención',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('21','Conocimiento de embarque por el servicio de transporte de carga marítima',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('22','Comprobante por Operaciones No Habituales',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('23','Pólizas de Adjudicación emitidas con ocasión del remate o adjudicación de bienes por venta forzada, por los martilleros o las entidades que rematen o subasten bienes por cuenta de terceros',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('24','Certificado de pago de regalías emitidas por PERUPETRO S.A',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('25','Documento de Atribución (Ley del Impuesto General a las Ventas e Impuesto Selectivo al Consumo, Art. 19º, último párrafo, R.S. N° 022-98-SUNAT).',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('26','Recibo por el Pago de la Tarifa por Uso de Agua Superficial con fines agrarios y por el pago de la Cuota para la ejecución de una determinada obra o actividad acordada por la Asamblea General de la Comisión de Regantes o Resolución expedida por el Jefe de la Unidad de Aguas y de Riego (Decreto Supremo N° 003-90-AG, Arts. 28 y 48)',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('27','Seguro Complementario de Trabajo de Riesgo',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('28','Tarifa Unificada de Uso de Aeropuerto',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('29','Documentos emitidos por la COFOPRI en calidad de oferta de venta de terrenos, los correspondientes a las subastas públicas y a la retribución de los servicios que presta',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('30','Documentos emitidos por las empresas que desempeñan el rol adquirente en los sistemas de pago mediante tarjetas de crédito y débito',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('31','Guía de Remisión - Transportista',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('32','Documentos emitidos por las empresas recaudadoras de la denominada Garantía de Red Principal a la que hace referencia el numeral 7.6 del artículo 7° de la Ley N° 27133  Ley de Promoción del Desarrollo de la Industria del Gas Natural',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('34','Documento del Operador',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('35','Documento del Partícipe',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('36','Recibo de Distribución de Gas Natural',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('37','Documentos que emitan los concesionarios del servicio de revisiones técnicas vehiculares, por la prestación de dicho servicio',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('40','Constancia de Depósito - IVAP (Ley 28211)',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('50','Declaración Única de Aduanas - Importación definitiva',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('52','Despacho Simplificado - Importación Simplificada',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('53','Declaración de Mensajería o Courier',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('54','Liquidación de Cobranza',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('87','Nota de Crédito Especial',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('88','Nota de Débito Especial',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('91','Comprobante de No Domiciliado',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('96','Exceso de crédito fiscal por retiro de bienes',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('97','Nota de Crédito - No Domiciliado',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('98','Nota de Débito - No Domiciliado',1);
INSERT INTO tipo_documento (codigo, descripcion, estado) VALUES ('99','Otros - Consolidado de Boletas de Venta',1);

-- inserts de 'moneda'
INSERT INTO moneda (codigo, nombre, estado) VALUES ('01', 'SOLES', 1);
INSERT INTO moneda (codigo, nombre, estado) VALUES ('02', 'DÓLARES', 1);

-- inserts de 'motivo'
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('01',0,'VENTA NACIONAL',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('02',1,'COMPRA NACIONAL',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('03',1,'CONSIGNACIÓN RECIBIDA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('04',0,'CONSIGNACIÓN ENTREGADA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('05',1,'DEVOLUCIÓN RECIBIDA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('06',0,'DEVOLUCIÓN ENTREGADA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('07',1,'BONIFICACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('08',1,'PREMIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('09',0,'DONACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('10',0,'SALIDA A PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('11',0,'SALIDA POR TRANSFERENCIA ENTRE ALMACENES ',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('12',0,'RETIRO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('13',0,'MERMAS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('14',0,'DESMEDROS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('15',0,'DESTRUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('16',1,'SALDO INICIAL',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('17',0,'EXPORTACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('18',1,'IMPORTACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('19',1,'ENTRADA DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('20',1,'ENTRADA POR DEVOLUCIÓN DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('21',1,'ENTRADA POR TRANSFERENCIA ENTRE ALMACENES ',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('22',1,'ENTRADA POR IDENTIFICACIÓN ERRONEA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('23',0,'SALIDA POR IDENTIFICACIÓN ERRONEA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('24',1,'ENTRADA POR DEVOLUCIÓN DEL CLIENTE',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('25',0,'SALIDA POR DEVOLUCIÓN AL PROVEEDOR',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('26',1,'ENTRADA PARA SERVICIO DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('27',0,'SALIDA POR SERVICIO DE PRODUCCIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('28',0,'AJUSTE POR DIFERENCIA DE INVENTARIO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('29',1,'ENTRADA DE BIENES EN PRÉSTAMO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('30',0,'SALIDA DE BIENES EN PRÉSTAMO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('31',1,'ENTRADA DE BIENES EN CUSTODIA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('32',0,'SALIDA DE BIENES EN CUSTODIA',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('33',1,'MUESTRAS MÉDICAS',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('34',0,'PUBLICIDAD',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('35',0,'GASTOS DE REPRESENTACIÓN',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('36',0,'RETIRO PARA ENTREGA A TRABAJADORES',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('37',0,'RETIRO POR CONVENIO COLECTIVO',1);
INSERT INTO motivo (codigo, tipo_movimiento, descripcion, estado) VALUES ('38',0,'RETIRO POR SUSTITUCIÓN DE BIEN SINIESTRADO',1);