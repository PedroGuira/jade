// Program.cs
using Jade.Data;
using Jade.Models;
using Jade.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Adicionado para IConfiguration se precisar injetar em outro lugar
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims; // Para as claims no AuthController (se for mover a l�gica para c�)

var builder = WebApplication.CreateBuilder(args);

// Adicionar DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Adicionar Identity
builder.Services.AddIdentity<UsuarioAdmin, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Ler configura��es JWT
var jwtKey = builder.Configuration["JwtSettings:Key"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];

// Valida��o das configura��es JWT
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("Chave JWT (JwtSettings:Key) n�o configurada em appsettings.json.");
}
if (string.IsNullOrEmpty(jwtIssuer))
{
    throw new InvalidOperationException("Issuer JWT (JwtSettings:Issuer) n�o configurado em appsettings.json.");
}
if (string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("Audience JWT (JwtSettings:Audience) n�o configurada em appsettings.json.");
}

// Configurar Autentica��o JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Para depura��o, ver eventos de falha na autentica��o
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("JWT Auth Failed: " + context.Exception.Message);
            Console.WriteLine("Exception Type: " + context.Exception.GetType().ToString());
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine("Inner Exception: " + context.Exception.InnerException.Message);
            }
            Console.WriteLine("Token que falhou (primeira parte): " + context.Request.Headers["Authorization"].ToString().Split('.').FirstOrDefault());
            Console.WriteLine("--------------------------------------------------");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("JWT Token Validated for: " + context.Principal.Identity.Name);
            foreach (var claim in context.Principal.Claims)
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }
            Console.WriteLine("--------------------------------------------------");
            return Task.CompletedTask;
        },
        OnChallenge = context => // Chamado quando a autoriza��o falha e um desafio � emitido
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("JWT OnChallenge: " + context.Error + " - " + context.ErrorDescription);
            // context.HandleResponse(); // Se voc� comentar isso, o erro 401 padr�o ser� enviado.
            // Se voc� descomentar e n�o fizer nada, pode obter uma resposta vazia ou diferente.
            // Melhor deixar comentado para ver o 401 padr�o.
            Console.WriteLine("--------------------------------------------------");
            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

// ... (resto dos seus services, controllers, Swagger, CORS) ...
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Jade API", Version = "v1" }); // Nome da API atualizado
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header. Exemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp",
        policyBuilder => // Renomeei builder para policyBuilder para evitar conflito com o WebApplicationBuilder
        {
            policyBuilder.WithOrigins("http://localhost:8080", "http://localhost:8081") // Suas portas do Vue
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IEstabelecimentoService, EstabelecimentoService>();
builder.Services.AddScoped<IGrupoOpcaoService, GrupoOpcaoService>();
builder.Services.AddScoped<IItemOpcaoService, ItemOpcaoService>();
builder.Services.AddScoped<IMarcaFranquiaService, MarcaFranquiaService>();
builder.Services.AddScoped<ILojaItemConfigService, LojaItemConfigService>();

var app = builder.Build();

// Seeding de Dados (Movido para uma fun��o para melhor organiza��o e chamado ap�s app.Build)
async Task SeedData(IHost appHost) // Recebe IHost para obter o escopo de servi�os
{
    using (var scope = appHost.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var userManager = services.GetRequiredService<UserManager<UsuarioAdmin>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
            // var dbContext = services.GetRequiredService<AppDbContext>(); // N�o precisamos mais do dbContext aqui para o seed b�sico

            Console.WriteLine("Iniciando seeding de dados...");

            string[] roleNames = { "SuperAdminSistema", "AdminMarcaFranquia", "AdminLoja" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                    Console.WriteLine($"Role '{roleName}' criada.");
                }
            }

            string superAdminEmail = "super@jade.com";
            string superAdminPassword = "PasswordSuper123!";
            var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
            if (superAdminUser == null)
            {
                superAdminUser = new UsuarioAdmin
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    NomeCompleto = "Super Administrador Jade",
                    EmailConfirmed = true,
                    Role = "SuperAdminSistema"
                };
                var createUserResult = await userManager.CreateAsync(superAdminUser, superAdminPassword);
                if (createUserResult.Succeeded)
                {
                    Console.WriteLine($"Usu�rio '{superAdminEmail}' criado.");
                    var addToRoleResult = await userManager.AddToRoleAsync(superAdminUser, "SuperAdminSistema");
                    if (addToRoleResult.Succeeded) Console.WriteLine($"Usu�rio '{superAdminEmail}' adicionado ao role 'SuperAdminSistema'.");
                    else Console.WriteLine($"Erro ao adicionar '{superAdminEmail}' ao role: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
                }
                else Console.WriteLine($"Erro ao criar usu�rio '{superAdminEmail}': {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
            }
            else
            {
                Console.WriteLine($"Usu�rio '{superAdminEmail}' j� existe.");
                if (!await userManager.IsInRoleAsync(superAdminUser, "SuperAdminSistema"))
                {
                    var addToRoleResult = await userManager.AddToRoleAsync(superAdminUser, "SuperAdminSistema");
                    if (addToRoleResult.Succeeded) Console.WriteLine($"Usu�rio '{superAdminEmail}' adicionado ao role 'SuperAdminSistema' (corre��o).");
                }
            }
            Console.WriteLine("Seeding de dados finalizado.");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Um erro ocorreu durante o seeding do banco de dados.");
        }
    }
}

// Chamar o seeding ap�s a constru��o do app
// Em um cen�rio de produ��o, voc� pode querer controlar isso com argumentos de linha de comando ou flags
if (app.Environment.IsDevelopment()) // Executar seeding apenas em desenvolvimento
{
    await SeedData(app); // Chamar a fun��o de seeding
}


// Configurar o pipeline de requisi��es HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jade API V1"); // Nome da API atualizado
    });
}

app.UseHttpsRedirection();
app.UseRouting(); // Deve vir antes de CORS, AuthN, AuthZ
app.UseCors("AllowVueApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();