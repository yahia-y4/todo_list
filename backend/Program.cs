using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ToDoApp.Data;
using ToDoApp.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

 


var builder = WebApplication.CreateBuilder(args);

// ====== Add DbContext =======
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ====== Add JWT Authentication =========
var key = "M5p#G7r!J2k&Z9q*L1v^B3x@N8d$T6e@"; 
var keyBytes = Encoding.UTF8.GetBytes(key);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();


var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();  
app.UseAuthorization();   


//  Login - إصدار توكن JWT

app.MapPost("/login", async (AppDbContext db, User user) =>
{
    var existUser = await db.Users.FirstOrDefaultAsync(u => u.Username == user.Username && u.Password == user.Password);
        

    if (existUser == null)
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, existUser.Username),
        new Claim(ClaimTypes.NameIdentifier, existUser.Id.ToString())
    };

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var jwt = tokenHandler.WriteToken(token);

    return Results.Ok(new { token = jwt });
});



// add new user 
app.MapPost("/register", async (AppDbContext db, User newUser) =>
{
    var exists = await db.Users.AnyAsync(u => u.Username == newUser.Username);
    if (exists)
        return Results.BadRequest("اسم المستخدم موجود مسبقًا");

    db.Users.Add(newUser);
    await db.SaveChangesAsync();
    return Results.Ok("تم إنشاء المستخدم بنجاح");
});

// add new Section 
app.MapPost("/section", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, Section newSection) =>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    int userId = int.Parse(userIdStr);
    newSection.UserId = userId;
    newSection.createDate = DateTime.Now;
    db.Sections.Add(newSection);
    await db.SaveChangesAsync();
    return Results.Ok(newSection);
});

// add new toDo
app.MapPost("/todo", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, ToDoTask newTodo) =>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    int userId = int.Parse(userIdStr);
    newTodo.UserId = userId;
    newTodo.CreateDate = DateTime.Now;
    db.ToDoTasks.Add(newTodo);
    await db.SaveChangesAsync();
    return Results.Ok(newTodo);

});


app.MapPut("/todo/id:int", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, ToDoTask newTodo) => { });

app.Run();
