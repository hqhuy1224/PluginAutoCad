# PluginAutoCad - GIS Layer Loader (WMS/WFS + Keycloak)

## Giới thiệu

Plugin AutoCAD dùng để load và hiển thị dữ liệu GIS từ GeoServer thông qua hai dịch vụ WMS và WFS, tích hợp đăng nhập Keycloak (SSO) để phân quyền người dùng.Có thể tải xuống file .exe trong thư mục Output để tiến hành cài đặt

## Tính năng

- Đăng nhập Keycloak (SSO - JWT)
- Load layer từ GeoServer WMS
  - http://localhost/geoserver/HoSoGis/wms
- Load layer từ GeoServer WFS
  - http://localhost/geoserver/HoSoGis/wfs
- Hiển thị dữ liệu GIS trực tiếp trong AutoCAD
- Phân quyền theo role (admin / editor / viewer)
- Tự động nhận diện loại service (WMS/WFS)
- Gửi token xác thực khi request GeoServer

## Kiến trúc hệ thống

Keycloak (Authentication Server)
        ↓
AutoCAD Plugin (.NET)
        ↓
GeoServer (WMS / WFS)
        ↓
Database GIS (PostGIS / File)

## Xác thực Keycloak

- Sử dụng OAuth2 / OpenID Connect
- Trả về JWT Token sau khi đăng nhập
- Token được attach vào header khi gọi GeoServer

## GeoServer Services

### WMS (Web Map Service)
http://localhost/geoserver/HoSoGis/wms

- Trả về dữ liệu dạng ảnh
- Dùng để hiển thị bản đồ nền trong AutoCAD

### WFS (Web Feature Service)
http://localhost/geoserver/HoSoGis/wfs

- Trả về dữ liệu vector (GML / GeoJSON)
- Dùng để vẽ và biên tập layer trong AutoCAD

## Tác giả

👨‍💻 Developed by: Huy Hoàng  
📌 Project: PluginAutoCad - GIS Layer Loader  
🔗 GitHub: https://github.com/hqhuy1224/PluginAutoCad
