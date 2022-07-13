CREATE TABLE `rtmart_exported_orders_tmp` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `order_number` varchar(45) DEFAULT NULL COMMENT '采购单号',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `rtmart_orders` (
  `id` int NOT NULL AUTO_INCREMENT,
  `store_location` varchar(255) DEFAULT NULL COMMENT '门店',
  `order_number` varchar(255) DEFAULT NULL COMMENT '采购单号',
  `order_type` varchar(255) DEFAULT NULL COMMENT '采购单类型',
  `order_status` varchar(255) DEFAULT NULL COMMENT '采购单状态',
  `product_code` varchar(255) DEFAULT NULL COMMENT '货号',
  `product_name` varchar(255) DEFAULT NULL COMMENT '品名',
  `size` varchar(255) DEFAULT NULL COMMENT '规格',
  `order_qty` varchar(255) DEFAULT NULL COMMENT '订购数量',
  `uom` varchar(255) DEFAULT NULL COMMENT '订购单位',
  `order_cases` varchar(255) DEFAULT NULL COMMENT '订购箱数',
  `price` varchar(255) DEFAULT NULL COMMENT '买价',
  `promotional_periods` varchar(255) DEFAULT NULL COMMENT '促销期数',
  `order_date` date DEFAULT NULL COMMENT '创单日期',
  `received_qty` varchar(255) DEFAULT NULL COMMENT '已收货数量',
  `must_arrived_by` date DEFAULT NULL COMMENT '预计到货日',
  `actual_received_at` date DEFAULT NULL COMMENT '实际收货日',
  `product_total_sales` varchar(45) DEFAULT NULL COMMENT '客户产品行总金额',
  `order_total_sales` varchar(45) DEFAULT NULL COMMENT '客户订单总金额',
  `region` varchar(45) DEFAULT NULL COMMENT '区域',
  `customer_name` varchar(45) DEFAULT NULL COMMENT '客户名称',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci