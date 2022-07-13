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
-- Table structure for table `walmart_orders`
--

DROP TABLE IF EXISTS `walmart_orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `walmart_orders` (
  `id` int NOT NULL AUTO_INCREMENT,
  `order_number` varchar(45) NOT NULL COMMENT '采购单号',
  `order_type` text COMMENT '采购单类型',
  `create_date` date DEFAULT NULL COMMENT '创建日期',
  `create_date_time` datetime DEFAULT NULL COMMENT '创建日期时间',
  `document_link` varchar(255) NOT NULL COMMENT 'document_link',
  `ship_date` date DEFAULT NULL COMMENT '起送日',
  `must_arrived_by` date DEFAULT NULL COMMENT '交货日',
  `promotional_event` varchar(45) DEFAULT NULL,
  `location` varchar(45) DEFAULT NULL,
  `allowance_or_charge` varchar(45) DEFAULT NULL,
  `allowance_description` varchar(45) DEFAULT NULL,
  `allowance_percent` varchar(45) DEFAULT NULL,
  `allowance_total` varchar(45) DEFAULT NULL,
  `total_order_amount_after_adjustments` decimal(20,6) DEFAULT NULL,
  `total_line_items` int DEFAULT NULL COMMENT 'item总数量',
  `total_units_ordered` int DEFAULT NULL COMMENT '总产品件数',
  `file_path` varchar(255) DEFAULT NULL COMMENT '订单文件路径',
  `line_number` varchar(45) NOT NULL COMMENT 'Line',
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
  `item_allowance_or_charge` varchar(45) DEFAULT NULL,
  `item_allowance_description` varchar(45) DEFAULT NULL,
  `item_allowance_qty` varchar(45) DEFAULT NULL,
  `allowance_uom` varchar(45) DEFAULT NULL,
  `item_allowance_percent` varchar(45) DEFAULT NULL,
  `item_allowance_total` varchar(45) DEFAULT NULL,
  `item_instructions` varchar(45) DEFAULT NULL,
  `customer_name` varchar(45) DEFAULT NULL COMMENT '平台商',
  `created_time` datetime DEFAULT NULL COMMENT '创建时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `orders_uniq_order_number_index` (`order_number`,`create_date_time`,`location`,`document_link`,`line_number`,`product_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-05-09 17:09:57
