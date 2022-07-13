-- MySQL dump 10.13  Distrib 8.0.27, for Win64 (x86_64)
--
-- Host: localhost    Database: vicode_wyeth
-- ------------------------------------------------------
-- Server version	8.0.27

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `clean_order_tracker`
--

DROP TABLE IF EXISTS `clean_order_tracker`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clean_order_tracker` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `customer_name` varchar(255) DEFAULT NULL COMMENT 'Customer Name',
  `dacang_account` varchar(255) DEFAULT NULL COMMENT '大仓账号',
  `dacang_password` varchar(255) DEFAULT NULL COMMENT '大仓密码',
  `payment_method` varchar(255) DEFAULT NULL COMMENT '付款方式（赊销/现金）',
  `order_capture_date` timestamp NULL DEFAULT NULL COMMENT '读单日期',
  `sold_to_code` varchar(255) DEFAULT NULL COMMENT 'SoldToCode',
  `ship_to_code` varchar(255) DEFAULT NULL COMMENT 'ShipToCode',
  `POID` varchar(255) DEFAULT NULL COMMENT 'POID（客户订单号）',
  `sku_code` varchar(255) DEFAULT NULL COMMENT '产品名称（惠氏SKU 代码）',
  `quantity` varchar(255) DEFAULT NULL COMMENT '数量（箱）',
  `isSuccess` varchar(255) DEFAULT NULL COMMENT '是否录单成功',
  `customer_order_number` varchar(45) DEFAULT NULL COMMENT '客户订单号',
  `order_doc_link` varchar(100) DEFAULT NULL COMMENT 'order document link',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1278 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Clean Order also DMS tracker';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `constraint_list`
--

DROP TABLE IF EXISTS `constraint_list`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `constraint_list` (
  `id` int NOT NULL AUTO_INCREMENT,
  `import_date` timestamp NULL DEFAULT NULL,
  `ver` bigint DEFAULT NULL,
  `sku_code` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL,
  `product_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL,
  `comment` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=668 DEFAULT CHARSET=utf8mb3 COLLATE=utf8_bin;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `exception_order`
--

DROP TABLE IF EXISTS `exception_order`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `exception_order` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `customer_name` varchar(45) DEFAULT NULL COMMENT '客户名称',
  `order_capture_date` varchar(45) DEFAULT NULL COMMENT 'RPA获取订单日期及时间',
  `order_date_time` varchar(45) DEFAULT NULL COMMENT '客户订单日期及时间',
  `requested_delivery_date` varchar(45) DEFAULT NULL COMMENT '客户订单计划到货日期',
  `dc_no` varchar(45) DEFAULT NULL COMMENT '门店/大仓编号',
  `customer_order_number` varchar(45) DEFAULT NULL COMMENT '客户订单号（POID）',
  `order_type` varchar(45) DEFAULT NULL COMMENT '订单类型/Event',
  `customer_sku` varchar(45) DEFAULT NULL COMMENT '客户产品编码',
  `customer_product_name` varchar(45) DEFAULT NULL COMMENT '客户产品名称',
  `customer_product_size` varchar(45) DEFAULT NULL COMMENT '客户产品规格',
  `customer_product_unit_count` varchar(45) DEFAULT NULL COMMENT '客户产品单位数量',
  `customer_product_qty` varchar(45) DEFAULT NULL COMMENT '客户产品箱数',
  `customer_product_unit_price` varchar(45) DEFAULT NULL COMMENT '客户产品单价',
  `customer_product_sales` varchar(45) DEFAULT NULL COMMENT '客户产品总价',
  `discount` varchar(45) DEFAULT NULL COMMENT '扣点',
  `real_discount` varchar(45) DEFAULT NULL COMMENT '实际扣点',
  `customer_order_sales` varchar(45) DEFAULT NULL COMMENT '客户订单总金额',
  `customer_order_sales_discount` varchar(45) DEFAULT NULL COMMENT '客户折后订单总金额',
  `customer_order_status` varchar(45) DEFAULT NULL COMMENT '客户订单状态（正常/取消）',
  `wyeth_sold_to` varchar(45) DEFAULT NULL COMMENT '惠氏客户Sold to',
  `wyeth_ship_to` varchar(45) DEFAULT NULL COMMENT '惠氏客户Ship to',
  `wyeth_customer_name` varchar(255) DEFAULT NULL COMMENT '惠氏客户名称',
  `wyeth_POID` varchar(45) DEFAULT NULL COMMENT '惠氏POID',
  `wyeth_sku` varchar(45) DEFAULT NULL COMMENT '惠氏产品编码',
  `wyeth_product_name` varchar(45) DEFAULT NULL COMMENT '惠氏产品名称',
  `wyeth_product_size` varchar(45) DEFAULT NULL COMMENT '惠氏产品规格',
  `wyeth_product_packages` varchar(45) DEFAULT NULL COMMENT '惠氏产品箱数',
  `wyeth_product_unit_price` varchar(45) DEFAULT NULL COMMENT '惠氏产品单价',
  `wyeth_product_nps` varchar(45) DEFAULT NULL COMMENT '惠氏产品箱价',
  `wyeth_order_sales` varchar(45) DEFAULT NULL COMMENT '惠氏订单总金额',
  `discount_order_sales` varchar(45) DEFAULT NULL COMMENT '折后订单总金额',
  `constraint_remark` varchar(45) DEFAULT NULL COMMENT '产品备注1（紧缺品）',
  `cxz_remark` varchar(45) DEFAULT NULL COMMENT '产品备注2（彩箱/整箱）',
  `exception_category` varchar(100) DEFAULT NULL COMMENT '异常分类',
  `exception_detail` text COMMENT '异常详细描述',
  `remark1` varchar(45) DEFAULT NULL COMMENT '备注',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1470 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `mail_setting`
--

DROP TABLE IF EXISTS `mail_setting`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mail_setting` (
  `id` int NOT NULL AUTO_INCREMENT,
  `order_category` varchar(255) DEFAULT NULL COMMENT '订单类型',
  `customer_name` varchar(255) DEFAULT NULL COMMENT '客户名称',
  `flow_name` varchar(45) DEFAULT NULL COMMENT '流程名',
  `mail_receipt_address` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci COMMENT '邮件接收人，英文分号（;）分割',
  `mail_cc_address` text COMMENT '邮件抄送人',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=51 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `material_master_data`
--

DROP TABLE IF EXISTS `material_master_data`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `material_master_data` (
  `id` int NOT NULL AUTO_INCREMENT,
  `customer_name` varchar(45) DEFAULT NULL COMMENT '客户名称',
  `ver` int DEFAULT NULL COMMENT '数据版本',
  `customer_material_no` varchar(45) DEFAULT NULL COMMENT '客户产品码',
  `customer_product_name` varchar(100) DEFAULT NULL COMMENT '客户产品名称',
  `customer_product_size` varchar(45) DEFAULT NULL COMMENT '客户产品规格',
  `customer_product_price` varchar(45) DEFAULT NULL COMMENT '客户产品价格',
  `customer_product_nps` varchar(45) DEFAULT NULL COMMENT '客户产品箱价',
  `wyeth_material_no` varchar(45) DEFAULT NULL COMMENT '惠氏产品码',
  `wyeth_product_name` varchar(80) DEFAULT NULL COMMENT '惠氏产品名称',
  `size` varchar(45) DEFAULT NULL COMMENT '规格',
  `wyeth_unit_price` varchar(45) DEFAULT NULL COMMENT '单价',
  `wyeth_nps` varchar(45) DEFAULT NULL COMMENT '箱价',
  `adjustive_price` varchar(45) DEFAULT NULL COMMENT '调价',
  `remark` varchar(45) DEFAULT NULL COMMENT '备注',
  `remark_option` text COMMENT '备注选项',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1690 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='产品主数据';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `metro_orders`
--

DROP TABLE IF EXISTS `metro_orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `metro_orders` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `supplier_card_no` varchar(45) DEFAULT NULL COMMENT '供商编码',
  `supplier` varchar(45) DEFAULT NULL COMMENT '供商名称',
  `store_or_dc_code` varchar(45) DEFAULT NULL COMMENT '门店/仓库编码',
  `store_or_dc_name` varchar(45) DEFAULT NULL COMMENT '门店/仓库名称',
  `order_number` varchar(45) NOT NULL COMMENT '订单号',
  `order_date` datetime NOT NULL COMMENT '订货日期',
  `request_delivery_date` varchar(45) NOT NULL COMMENT '计划到货日期',
  `metro_hq_product_code` varchar(45) DEFAULT NULL COMMENT '麦德龙总部商品编码',
  `product_name` varchar(100) DEFAULT NULL COMMENT '商品名称',
  `product_code` varchar(45) DEFAULT NULL COMMENT '商品编码',
  `gtin_code` varchar(45) DEFAULT NULL COMMENT '国条',
  `category` varchar(45) DEFAULT NULL COMMENT '种类',
  `supplier_product_no` varchar(45) DEFAULT NULL COMMENT '供应商商品编号',
  `quantity` varchar(45) DEFAULT NULL COMMENT '订货量',
  `size` varchar(45) DEFAULT NULL COMMENT '订货单位',
  `zipcode` varchar(45) DEFAULT NULL COMMENT '邮报编码',
  `cpo_no` varchar(45) DEFAULT NULL COMMENT 'CPO单号',
  `dc_no` varchar(45) DEFAULT NULL COMMENT '大仓编码',
  `order_status` varchar(45) DEFAULT NULL COMMENT '订单状态',
  `business_type` varchar(45) DEFAULT NULL COMMENT '业务类型',
  `order_type` varchar(45) NOT NULL COMMENT '订单类型',
  `print_status` varchar(45) DEFAULT NULL COMMENT '打印状态',
  `confirm_way` varchar(45) DEFAULT NULL COMMENT '确认方式',
  `order_print_PO` varchar(45) DEFAULT NULL COMMENT '订单PrintPO',
  `wyeth_nps` varchar(45) DEFAULT NULL COMMENT '惠氏箱价',
  `wyeth_total_sales` varchar(45) DEFAULT NULL COMMENT '总计',
  `wyeth_po_number` varchar(45) DEFAULT NULL COMMENT 'PO number',
  `wyeth_sku` varchar(45) DEFAULT NULL COMMENT '惠氏sku',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1063 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='麦德龙订单';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `order_fecthing_records`
--

DROP TABLE IF EXISTS `order_fecthing_records`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `order_fecthing_records` (
  `id` int NOT NULL AUTO_INCREMENT,
  `customer_name` varchar(45) DEFAULT NULL COMMENT '客户名称',
  `cur_date` date DEFAULT NULL,
  `times` int DEFAULT NULL COMMENT '当日采集批次',
  `fetch_status` tinyint(1) DEFAULT '0' COMMENT '此批次订单抓取状态',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=43 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='订单采集批次';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `password_email_setting`
--

DROP TABLE IF EXISTS `password_email_setting`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `password_email_setting` (
  `id` int NOT NULL AUTO_INCREMENT,
  `customer_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL,
  `dacang_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL,
  `dacang_account` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL,
  `dacang_password` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL,
  `Email_setting` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=66 DEFAULT CHARSET=utf8mb3 COLLATE=utf8_bin;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `rpa_accounts`
--

DROP TABLE IF EXISTS `rpa_accounts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rpa_accounts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `customer_name` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT '客户名称',
  `flow_name` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT '流程名称',
  `user_name` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT '用户名',
  `password` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT '密码',
  `customer_login_url` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT '客户系统登录链接',
  `flow_alert_receiver_email_address` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci COMMENT '流程报警接收邮箱号',
  `region` varchar(45) DEFAULT NULL COMMENT '区域',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ship_to_sold_to`
--

DROP TABLE IF EXISTS `ship_to_sold_to`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ship_to_sold_to` (
  `id` int NOT NULL AUTO_INCREMENT,
  `customer_name` varchar(45) DEFAULT NULL COMMENT '客户名称',
  `dc_name` varchar(45) DEFAULT NULL COMMENT 'DC',
  `dc_no` varchar(45) DEFAULT NULL COMMENT '门店/仓库编码',
  `store_location` varchar(45) DEFAULT NULL COMMENT '门店',
  `ship_to` varchar(45) DEFAULT NULL COMMENT 'Ship to',
  `sold_to` varchar(45) DEFAULT NULL COMMENT 'Sold to',
  `wyeth_dc_no` varchar(45) DEFAULT NULL COMMENT '惠氏大仓代码',
  `region` varchar(45) DEFAULT NULL COMMENT '区域',
  `customer_pay_method` varchar(45) DEFAULT NULL COMMENT '客户付款方式',
  `dms_account` varchar(45) DEFAULT NULL COMMENT 'DMS 账号',
  `discount` varchar(45) DEFAULT NULL COMMENT '扣点',
  `top_customer_name` varchar(45) DEFAULT NULL COMMENT '上层客户名',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=52 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `special_products`
--

DROP TABLE IF EXISTS `special_products`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `special_products` (
  `id` int NOT NULL AUTO_INCREMENT,
  `customer_name` varchar(45) DEFAULT NULL COMMENT '客户名称',
  `sold_to` varchar(45) DEFAULT NULL COMMENT 'Sold To',
  `dc_no` varchar(45) DEFAULT NULL COMMENT '大仓号',
  `customer_sku_code` varchar(45) DEFAULT NULL COMMENT '客户产品码',
  `sku_code` varchar(45) DEFAULT NULL COMMENT 'SKU CODE',
  `product_name` varchar(45) DEFAULT NULL COMMENT '产品名称',
  `comment` varchar(45) DEFAULT NULL COMMENT '描述',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=62 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='特殊产品';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tracker`
--

DROP TABLE IF EXISTS `tracker`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tracker` (
  `id` int NOT NULL AUTO_INCREMENT,
  `dacang_account` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT '大仓账号',
  `dacang_password` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT '大仓密码',
  `payment_method` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT '付款方式',
  `order_capture_date` timestamp NULL DEFAULT NULL COMMENT '读单日期',
  `sold_to_code` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT 'SoldToCode',
  `ship_to_code` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT 'ShipToCode',
  `customer_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT '客户名称',
  `POID` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT '客户订单号',
  `sku_code` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT '产品名称\r\n（惠氏SKU 代码）',
  `quantity` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT '数量\r\n(箱）',
  `isSuccess` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin DEFAULT NULL COMMENT '是否录单成功',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4813 DEFAULT CHARSET=utf8mb3 COLLATE=utf8_bin;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `walmart_exported_orders`
--

DROP TABLE IF EXISTS `walmart_exported_orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `walmart_exported_orders` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `order_number` varchar(45) DEFAULT NULL COMMENT 'Document Number',
  `document_type` varchar(45) DEFAULT NULL COMMENT 'Document Type',
  `received_date_time` datetime DEFAULT NULL COMMENT 'Received Date',
  `vendor_number` varchar(45) DEFAULT NULL COMMENT 'Vendor Number',
  `location` varchar(45) DEFAULT NULL COMMENT 'Location',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `walmart_exported_orders_unique_order_received_time_index` (`order_number`,`received_date_time`,`location`) /*!80000 INVISIBLE */
) ENGINE=InnoDB AUTO_INCREMENT=9327 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='用于walmart 导出的Exce格式l转存的orders，需要导入一段时间的初始数据';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `walmart_exported_orders_tmp`
--

DROP TABLE IF EXISTS `walmart_exported_orders_tmp`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `walmart_exported_orders_tmp` (
  `id` int NOT NULL AUTO_INCREMENT COMMENT '主键',
  `order_number` varchar(45) DEFAULT NULL COMMENT 'Document Number',
  `document_type` varchar(45) DEFAULT NULL COMMENT 'Document Type',
  `received_date_time` datetime DEFAULT NULL COMMENT 'Received Date',
  `vendor_number` varchar(45) DEFAULT NULL COMMENT 'Vendor Number',
  `location` varchar(45) DEFAULT NULL COMMENT 'Location',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=280 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='用于walmart 每次导出的Excel格式的orders，跟walmart_exported_orders 比较得到增量数据';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `walmart_orders`
--

DROP TABLE IF EXISTS `walmart_orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `walmart_orders` (
  `id` int NOT NULL AUTO_INCREMENT,
  `order_number` varchar(45) NOT NULL COMMENT 'order_number',
  `order_type` text COMMENT 'order_type',
  `create_date` date DEFAULT NULL COMMENT 'create_date',
  `create_date_time` datetime DEFAULT NULL COMMENT 'create_date_time',
  `document_link` varchar(255) NOT NULL COMMENT 'document_link',
  `ship_date` date DEFAULT NULL COMMENT 'ship_date',
  `must_arrived_by` date DEFAULT NULL COMMENT 'must_arrived_by',
  `promotional_event` varchar(45) DEFAULT NULL COMMENT 'promotional_event',
  `location` varchar(45) DEFAULT NULL COMMENT 'location',
  `allowance_or_charge` varchar(45) DEFAULT NULL COMMENT 'allowance_or_charge',
  `allowance_description` varchar(45) DEFAULT NULL COMMENT 'allowance_description',
  `allowance_percent` varchar(45) DEFAULT NULL COMMENT 'allowance_percent',
  `allowance_total` varchar(45) DEFAULT NULL COMMENT 'allowance_total',
  `total_order_amount_after_adjustments` decimal(20,6) DEFAULT NULL COMMENT 'total_order_amount_after_adjustments',
  `total_line_items` int DEFAULT NULL COMMENT 'total_line_items',
  `total_units_ordered` int DEFAULT NULL COMMENT 'total_units_ordered',
  `file_path` varchar(255) DEFAULT NULL COMMENT 'file_path',
  `line_number` varchar(45) CHARACTER SET armscii8 COLLATE armscii8_general_ci NOT NULL COMMENT 'Line',
  `product_code` varchar(45) NOT NULL COMMENT 'Item',
  `gtin` varchar(45) DEFAULT NULL COMMENT 'GTIN',
  `supplier_stock` varchar(45) DEFAULT NULL COMMENT 'Supplier Stock #',
  `color` varchar(45) DEFAULT NULL COMMENT 'Color',
  `size` varchar(45) DEFAULT NULL COMMENT 'Size',
  `quantity_ordered` varchar(45) DEFAULT NULL COMMENT 'Quantity Ordered',
  `uom` varchar(45) DEFAULT NULL COMMENT 'UOM',
  `pack` varchar(45) DEFAULT NULL COMMENT 'Pack',
  `cost` varchar(45) DEFAULT NULL COMMENT 'Cost',
  `extended_cost` varchar(45) DEFAULT NULL COMMENT 'Extended Cost',
  `item_description` varchar(45) DEFAULT NULL COMMENT 'itemDescription',
  `tax_type` varchar(45) DEFAULT NULL COMMENT 'taxType',
  `tax_percent` varchar(45) DEFAULT NULL COMMENT 'taxPercent',
  `item_allowance_or_charge` varchar(45) DEFAULT NULL COMMENT 'item_allowance_or_charge',
  `item_allowance_description` varchar(45) DEFAULT NULL COMMENT 'item_allowance_or_charge',
  `item_allowance_qty` varchar(45) DEFAULT NULL COMMENT 'item_allowance_qty',
  `item_allowance_uom` varchar(45) DEFAULT NULL COMMENT 'item_allowance_uom',
  `item_allowance_percent` varchar(45) DEFAULT NULL COMMENT 'item_allowance_percent',
  `item_allowance_total` varchar(45) DEFAULT NULL COMMENT 'item_allowance_total',
  `item_instructions` varchar(45) DEFAULT NULL COMMENT 'item_instructions',
  `customer_name` varchar(45) DEFAULT NULL COMMENT '平台商',
  `wyeth_poid` varchar(100) DEFAULT NULL COMMENT '惠氏订单号',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `orders_uniq_order_number_index` (`order_number`,`create_date_time`,`location`,`document_link`,`line_number`,`product_code`)
) ENGINE=InnoDB AUTO_INCREMENT=3015 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `wumart_exported_orders`
--

DROP TABLE IF EXISTS `wumart_exported_orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `wumart_exported_orders` (
  `id` int NOT NULL,
  `order_number` varchar(45) DEFAULT NULL COMMENT '订单号',
  `dc_no` varchar(45) DEFAULT NULL COMMENT '大仓号',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `wumart_exported_orders_tmp`
--

DROP TABLE IF EXISTS `wumart_exported_orders_tmp`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `wumart_exported_orders_tmp` (
  `id` int NOT NULL,
  `order_number` varchar(45) DEFAULT NULL COMMENT '订单号',
  `dc_no` varchar(45) DEFAULT NULL COMMENT '大仓号',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `wumart_orders`
--

DROP TABLE IF EXISTS `wumart_orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `wumart_orders` (
  `id` int NOT NULL AUTO_INCREMENT,
  `district_no` varchar(45) DEFAULT NULL COMMENT '大区号',
  `order_number` varchar(100) DEFAULT NULL COMMENT '订单号',
  `delivery_number` varchar(100) DEFAULT NULL COMMENT '送货单号',
  `order_location` varchar(45) DEFAULT NULL COMMENT '订货地',
  `ship_to` varchar(255) DEFAULT NULL COMMENT '收货地址',
  `dc_no` varchar(45) DEFAULT NULL COMMENT '大仓号',
  `dc_name` varchar(45) DEFAULT NULL COMMENT '大仓名',
  `order_date` datetime DEFAULT NULL COMMENT '实际订单日期',
  `order_type` varchar(45) DEFAULT NULL COMMENT '订单类型',
  `supplier_card_no` varchar(45) DEFAULT NULL COMMENT '供商卡号',
  `supplier_name` varchar(45) DEFAULT NULL COMMENT '供商名称',
  `estimated_arrived_at` datetime DEFAULT NULL COMMENT 'RDD',
  `shelf_group_and_name` varchar(45) DEFAULT NULL COMMENT '货架组以及名称',
  `product_code` varchar(45) DEFAULT NULL COMMENT '商品编号',
  `product_name` varchar(255) DEFAULT NULL COMMENT '商品名称',
  `gtin` varchar(45) DEFAULT NULL COMMENT '商品条码',
  `price` varchar(45) DEFAULT NULL COMMENT '价格',
  `order_qty` varchar(45) DEFAULT NULL COMMENT '订货量',
  `package_uom` varchar(45) DEFAULT NULL COMMENT '包装单位',
  `producing_area` varchar(255) DEFAULT NULL COMMENT '产地',
  `if_scattered` varchar(45) DEFAULT NULL COMMENT '拆零',
  `customer_product_notax_total` varchar(45) DEFAULT NULL COMMENT '物美总价',
  `customer_product_notax_price` varchar(45) DEFAULT NULL COMMENT '物美未税单价',
  `customer_size` varchar(45) DEFAULT NULL COMMENT '物美规格',
  `customer_order_notax_price` varchar(45) DEFAULT NULL COMMENT '物美订单总金额',
  `created_time` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_time` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-06-10 18:06:16
