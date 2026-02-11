# Especificación Técnica - Sistema de Arqueo de Caja

## 📋 Tabla de Contenidos

1. [Visión General](#visión-general)
2. [Arquitectura del Sistema](#arquitectura-del-sistema)
3. [Componentes](#componentes)
4. [Flujo de Datos](#flujo-de-datos)
5. [Especificación de APIs](#especificación-de-apis)
6. [Configuración y Despliegue](#configuración-y-despliegue)
7. [Manejo de Errores](#manejo-de-errores)
8. [Consideraciones de Seguridad](#consideraciones-de-seguridad)
9. [Troubleshooting](#troubleshooting)

---

## Visión General

### Propósito
Sistema distribuido para consultar información de arqueo de caja desde una base de datos SQL Server remota a través de una arquitectura de microservicios.

### Stack Tecnológico
- **Backend API**: .NET 8 (ASP.NET Core Minimal API)
- **SQL Bridge**: .NET 8 (Windows Service)
- **Base de Datos**: SQL Server 2019+
- **Serialización**: System.Text.Json
- **Hosting**: Railway (API) + Windows Server (SQL Bridge)
- **Proxy Reverso**: Configurado para `https://sql.comerciopele.com.ar`

---

## Arquitectura del Sistema

### Diagrama de Componentes


