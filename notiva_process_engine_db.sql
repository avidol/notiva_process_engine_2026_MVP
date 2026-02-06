-- MySQL dump 10.13  Distrib 8.0.42, for Win64 (x86_64)
--
-- Host: localhost    Database: notiva_process_engine_db
-- ------------------------------------------------------
-- Server version	8.0.42

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
-- Table structure for table `notifications`
--

DROP TABLE IF EXISTS `notifications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `notifications` (
  `id` char(36) NOT NULL,
  `channel` varchar(20) NOT NULL,
  `state` varchar(20) NOT NULL,
  `retry_count` int NOT NULL,
  `max_retry` int NOT NULL,
  `next_retry_at` datetime DEFAULT NULL,
  `payload_json` json NOT NULL,
  `last_error` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `rule_violations` json DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `notifications`
--

LOCK TABLES `notifications` WRITE;
/*!40000 ALTER TABLE `notifications` DISABLE KEYS */;
INSERT INTO `notifications` VALUES ('03ba1667-eaa8-4124-b6e9-8da62493307d','0','4',0,5,NULL,'{\"raw\": \"{ \\\"policyNumber\\\": \\\"POL777777\\\" }\", \"queue\": \"notiva.in.q\", \"source\": \"RabbitMQ\", \"receivedAt\": \"2026-02-02T08:41:46.8705807Z\"}',NULL,'2026-02-02 14:11:47','2026-02-02 14:11:47',NULL),('27ebe958-6f9f-48a4-9a65-eb88d78c7da3','0','4',0,5,NULL,'{\"raw\": \"{ \\\"policyNumber\\\": \\\"POL55555\\\" }\", \"queue\": \"notiva.in.q\", \"source\": \"RabbitMQ\", \"receivedAt\": \"2026-02-02T06:57:18.8155546Z\"}',NULL,'2026-02-02 12:27:18','2026-02-02 12:27:18',NULL),('312b96e0-a916-48b4-b8a4-0a712c9cadb0','0','4',11,5,'2026-02-02 12:27:36','{\"raw\": \"{ \\\"policyNumber\\\": \\\"POL44444\\\" }\", \"queue\": \"notiva.in.q\", \"source\": \"RabbitMQ\", \"receivedAt\": \"2026-02-01T16:27:23.4338791Z\"}','The process cannot access the file \'E:\\Globsyn\\projects\\ProcessEngine.Worker\\audit\\rule-engine.log\' because it is being used by another process.','2026-02-01 21:57:23','2026-02-01 21:57:23',NULL),('c7aac6ab-4b12-4055-b8f8-6fee12e235bd','0','4',11,5,'2026-02-02 12:27:36','{\"raw\": \"{ \\\"policyNumber\\\": \\\"POL33333\\\" }\", \"queue\": \"notiva.in.q\", \"source\": \"RabbitMQ\", \"receivedAt\": \"2026-02-01T16:20:05.1646992Z\"}','The process cannot access the file \'E:\\Globsyn\\projects\\ProcessEngine.Worker\\audit\\rule-engine.log\' because it is being used by another process.','2026-02-01 21:50:05','2026-02-01 21:50:05',NULL),('e3fb3977-643f-4333-ad8b-a2ef691b389d','0','4',7,5,'2026-02-01 22:19:00','{\"raw\": \"{ \\\"policyNumber\\\": \\\"POL44444\\\" }\", \"queue\": \"notiva.in.q\", \"source\": \"RabbitMQ\", \"receivedAt\": \"2026-02-01T16:44:16.2753824Z\"}','The SMTP server requires a secure connection or the client was not authenticated. The server response was: 5.7.0 Authentication Required. For more information, go to','2026-02-01 22:14:16','2026-02-01 22:14:16',NULL);
/*!40000 ALTER TABLE `notifications` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-02-06 16:16:41
