using Microsoft.AspNetCore.Authentication.Cookies;


namespace SecureAuth.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. ��������� �������� 

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews(); // ���������� � ���������� ��������, ����������� ��� ������ �� ������� MVC

            // ���������� � ��������� �������� �������������� �� ������ Cookie
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login"; // ��������������� ��������������������� ������������� ��� ������� ��������� ������� � ����������� �������
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // ��������� ������� ����� Cookie � 30 �����
                options.SlidingExpiration = true; // ��������� ������� ����� Cookie ��� ���������� ������������
            });

            // 2. ��������� ��������� ��������� ��������

            var app = builder.Build(); // ������ ����������� �������� � ������� ���������� 

            app.UseStatusCodePagesWithReExecute("/Error/{0}"); // ��������� ��������� ����� ��������� (404 � ������)

            if (!app.Environment.IsDevelopment()) // ���������� ������ � Production-������ (�� ��� ����������)
            {
                app.UseExceptionHandler("/Home/Error"); // ��������� ����������� ����������� ����������
                app.UseHsts(); // ���������  HSTS, ������������� ������� �������� � ������ ������ �� HTTPS
            }

            app.UseHttpsRedirection(); // ��������������� ���� HTTP-�������� �� HTTPS
            app.UseStaticFiles(); // ����������� ������� �������� ����������� ����� (CSS, JS) �� ����� wwwroot
            app.UseRouting(); // ����� ��������� (������ �����������) ��� ��������� �������
            app.UseAuthentication(); // �������������� 
            app.UseAuthorization(); // �����������

            app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}"); // ��������� ������������� �� ��������� ��� MVC

            app.Run(); // ������ ����������
        }
    }
}
