# OrderManagement MCP Server

## Configuration với User Secrets

Project này sử dụng **User Secrets** để lưu trữ cấu hình nhạy cảm (sensitive data) như API keys, connection strings, v.v.

### Tại sao dùng User Secrets?

- ✅ Không commit sensitive data lên Git
- ✅ Mỗi developer có config riêng trên máy local
- ✅ Tự động được load trong Development environment
- ✅ Override các giá trị trong `appsettings.json`

### Configuration Priority (từ cao xuống thấp)

1. **Command-line arguments** - Tham số khi chạy app
2. **Environment variables** - Biến môi trường hệ thống
3. **User Secrets** - Lưu local, chỉ trong Development
4. **appsettings.{Environment}.json** - Config theo môi trường
5. **appsettings.json** - Config mặc định

### Các lệnh User Secrets

#### 1. Xem danh sách secrets hiện tại
```bash
dotnet user-secrets list
```

#### 2. Thêm/Cập nhật một secret
```bash
dotnet user-secrets set "ApplicationInsights:ConnectionString" "InstrumentationKey=xxxx;..."
dotnet user-secrets set "SendGrid:ApiKey" "SG.xxxxxxxxxxxxx"
```

#### 3. Xóa một secret
```bash
dotnet user-secrets remove "ApplicationInsights:ConnectionString"
```

#### 4. Xóa tất cả secrets
```bash
dotnet user-secrets clear
```

### Ví dụ cấu hình cần thiết

```bash
# Application Insights (Observability)
dotnet user-secrets set "ApplicationInsights:ConnectionString" "InstrumentationKey=ae14ef3f-e76c-42d2-805e-cd732f92869e;IngestionEndpoint=https://southeastasia-1.in.applicationinsights.azure.com/;..."

# SendGrid (Email service)
dotnet user-secrets set "SendGrid:ApiKey" "SG.your-api-key-here"

# Database (nếu cần override)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=OrderManagementDb;..."
```

### Vị trí lưu trữ User Secrets

Secrets được lưu tại:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

Trong project này, `<user_secrets_id>` là: `b10fb15f-ae0b-4792-a2ff-98b8e04b4ada`

### Verify configuration đang được sử dụng

Khi chạy application, bạn có thể kiểm tra logs để xác nhận User Secrets đã được load:

```bash
# Set environment là Development
$env:ASPNETCORE_ENVIRONMENT='Development'

# Run application
dotnet run

# Hoặc chỉ định environment khi chạy
dotnet run --environment Development
```

### Production Deployment

⚠️ **Lưu ý**: User Secrets chỉ dùng cho Development!

Khi deploy lên Production, sử dụng:
- **Azure App Service**: Application Settings / Configuration
- **Docker**: Environment Variables
- **Kubernetes**: ConfigMaps & Secrets
- **Azure Key Vault**: Để lưu trữ secrets an toàn

## Cấu hình mẫu trong appsettings.json

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=.;Database=OrderManagementDb;..."
  },
  "ApplicationInsights": {
	"ConnectionString": ""  // ← Sẽ được override bởi User Secrets
  },
  "SendGrid": {
	"ApiKey": "REPLACE_WITH_USER_SECRETS",  // ← Sẽ được override
	"SenderEmail": "orders@tedu.com.vn",
	"SenderName": "TEDU Order System"
  }
}
```

## Troubleshooting

### Secrets không được load?

1. Kiểm tra environment:
   ```bash
   echo $env:ASPNETCORE_ENVIRONMENT  # Windows
   echo $ASPNETCORE_ENVIRONMENT      # Linux/macOS
   ```

2. Kiểm tra UserSecretsId trong `.csproj`:
   ```xml
   <PropertyGroup>
	 <UserSecretsId>b10fb15f-ae0b-4792-a2ff-98b8e04b4ada</UserSecretsId>
   </PropertyGroup>
   ```

3. Verify secrets file tồn tại:
   ```bash
   # Windows
   cat $env:APPDATA\Microsoft\UserSecrets\b10fb15f-ae0b-4792-a2ff-98b8e04b4ada\secrets.json

   # macOS/Linux
   cat ~/.microsoft/usersecrets/b10fb15f-ae0b-4792-a2ff-98b8e04b4ada/secrets.json
   ```

### Secret bị lỗi format?

User Secrets sử dụng format JSON hierarchy với dấu `:` hoặc `__`:

```bash
# Cách 1: dùng dấu ":"
dotnet user-secrets set "Section:SubSection:Key" "value"

# Cách 2: dùng dấu "__"
dotnet user-secrets set "Section__SubSection__Key" "value"
```

## Tài liệu tham khảo

- [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
