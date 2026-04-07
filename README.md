---

# 📦 POPSManager

Conversor y organizador automático de juegos PS1/PS2 para OPL + POPStarter

POPSManager es una herramienta moderna y automatizada diseñada para preparar juegos de PlayStation 1 y PlayStation 2 para su uso en Open PS2 Loader (OPL) y POPStarter, generando toda la estructura necesaria sin requerir conocimientos técnicos del usuario.

Su objetivo es ofrecer un flujo rápido, seguro, legal y totalmente automatizado.

---

## 🚀 Características principales

### 🎮 PS1 → Conversión automática a VCD
- Convierte BIN/CUE/ISO a formato VCD compatible con POPStarter.  
- Genera nombres limpios y profesionales.  
- Detecta automáticamente juegos multidisco.  
- Crea carpetas POPS y subcarpetas por juego.  
- Genera BOOT.ELF para cada juego PS1 en la carpeta APPS/.  
- Crea automáticamente el archivo DISCS.TXT para multidisco.

---

### 🎮 PS2 → Copia directa
- Los juegos PS2 no se convierten.  
- Se copian directamente a la carpeta DVD/.  
- Se detectan automáticamente ISOs PS2 para evitar conversiones erróneas.

---

### 🗂️ Estructura OPL generada automáticamente
POPSManager crea todas las carpetas necesarias si no existen:

`
/POPS
/DVD
/APPS
/ART
/CFG
`

---

### 🔍 Detección inteligente
- Detecta Game ID sin extraer contenido protegido.  
- Limpia nombres automáticamente.  
- Detecta número de disco (CD1, CD2, Disc 1, etc.).  
- Evita conversiones innecesarias (PS2 ISO).  

---

### 🧩 Multidisco totalmente soportado
POPStarter requiere un archivo DISCS.TXT para enlazar discos.  
POPSManager lo genera automáticamente:

`
mass:/POPS/SLUS12345 (CD1)/SLUS12345 (CD1).VCD
mass:/POPS/SLUS12345 (CD2)/SLUS12345 (CD2).VCD
`

---

### 🖥️ Interfaz moderna y clara
- Notificaciones tipo toast.  
- Barra de progreso global.  
- Logs en tiempo real.  
- Animaciones suaves.  
- Vistas separadas para cada tarea.

---

### 🔒 Cumplimiento legal
POPSManager no extrae, modifica ni distribuye contenido protegido.  
Todo se basa en:
- Conversión de archivos proporcionados por el usuario.  
- Generación de metadatos y archivos auxiliares.  
- Detección de nombres y patrones, no de contenido interno.  

---

## 📁 Estructura generada

### PS1 (POPStarter)
`
/POPS/SLUS12345 (CD1)/SLUS12345 (CD1).VCD
/POPS/SLUS_12345 (CD1)/DISCS.TXT
/APPS/SLUS_12345.ELF
`

### PS2 (OPL)
`
/DVD/SLUS_99999.ISO
`

---

## 🧠 Cómo funciona internamente

### 1. Conversión PS1
- Lee sectores de 2352 bytes.  
- Extrae solo los 2048 bytes de datos.  
- Escribe encabezado POPStarter.  
- Genera VCD optimizado.

### 2. Detección PS2
- Lee primeros 32 KB del ISO.  
- Busca cadenas como PLAYSTATION 2 o BOOT2.

### 3. Multidisco
- Detecta patrones:  
  - CD1, CD2, Disc 1, Disc2, Disk3  
- Ordena discos.  
- Genera DISCS.TXT.

### 4. Generación de ELF
- Crea un ELF por juego PS1.  
- No modifica contenido del juego.  
- Compatible con POPStarter.

---

## 🛠️ Requisitos
- Windows 10/11  
- .NET 8  
- Juegos en formato BIN/CUE/ISO  
- Carpeta de destino para OPL (USB/HDD/SMB)

---

## 📸 Capturas (opcional)
![Vista del proyecto](https://copilot.microsoft.com/th/id/BCO.6c5ac29b-157b-45f0-ac86-5810992bc20e.png)

---

## 🧩 Estado del proyecto
POPSManager está en desarrollo activo y orientado a ofrecer una experiencia profesional, estable y completamente automatizada para usuarios de OPL y POPStarter.

---

## 🤝 Contribuciones
Las contribuciones son bienvenidas.  
Puedes enviar:
- Pull requests  
- Reportes de errores  
- Ideas de mejora  

---

## 📜 Licencia
Este proyecto no distribuye contenido protegido por copyright.  
El usuario es responsable de los archivos que procesa.

---

## 🙌 Autor
Desarrollado por Raidel, con enfoque en automatización, UX moderna y compatibilidad total con OPL + POPStarter.

---
