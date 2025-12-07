# Hướng dẫn Deploy lên Railway

## Bước 1: Chuẩn bị trên Railway

1. Đăng nhập vào [Railway](https://railway.app)
2. Click **"New Project"**
3. Chọn **"Deploy from GitHub repo"**
4. Chọn repository: `suuboy11111/MATT`
5. Railway sẽ tự động detect project và bắt đầu build

## Bước 2: Cấu hình Database (PostgreSQL)

1. Trong project trên Railway, click **"New"** → **"Database"** → **"Add PostgreSQL"**
2. Railway sẽ tự động tạo PostgreSQL database
3. Copy **Connection String** từ PostgreSQL service

## Bước 3: Cấu hình Environment Variables

Trong service web của bạn, vào tab **"Variables"** và thêm các biến sau:

### Connection String
```
DATABASE_URL=<Connection string từ PostgreSQL service>
```

Hoặc nếu Railway tự động tạo biến `DATABASE_URL`, bạn cần map nó vào `ConnectionStrings__DefaultConnection`:

```
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
```

### PayOS Configuration (nếu cần)
```
PayOS__ClientId=9ca8c566-b2e8-4497-88fc-a5ad18f477f8
PayOS__ApiKey=4209e4e9-a757-4104-ad73-d21d18e9037a
PayOS__ChecksumKey=05a4aafcabab2416009875d0b95b999f5faa6827a08562b2fa2972ef3b5b55ab
```

### ASP.NET Core Environment
```
ASPNETCORE_ENVIRONMENT=Production
```

## Bước 4: Cấu hình Port

Railway tự động set biến `PORT`. Code đã được cấu hình để sử dụng biến này.

## Bước 5: Deploy

1. Railway sẽ tự động deploy khi bạn push code lên GitHub
2. Hoặc bạn có thể click **"Deploy"** trong Railway dashboard
3. Đợi build và deploy hoàn tất
4. Click vào service để xem logs và URL

## Bước 6: Kiểm tra

1. Sau khi deploy thành công, Railway sẽ cung cấp URL (ví dụ: `your-app.railway.app`)
2. Truy cập URL để kiểm tra website
3. Kiểm tra logs nếu có lỗi

## Lưu ý quan trọng:

1. **Database Migration**: Code đã tự động chạy migration khi khởi động
2. **Static Files**: Đảm bảo các file trong `wwwroot` được commit vào git
3. **Environment Variables**: Không commit file `appsettings.json` có chứa thông tin nhạy cảm
4. **Port**: Railway tự động set PORT, không cần cấu hình thủ công

## Troubleshooting:

- **Build failed**: Kiểm tra logs trong Railway dashboard
- **Database connection error**: Kiểm tra Connection String và biến môi trường
- **Static files không load**: Đảm bảo `wwwroot` folder được commit
- **Port error**: Kiểm tra `railway.json` và biến `PORT`



