using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ToDoApp.Data;
using ToDoApp.Models;


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


//  Login -

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

// get user name 
app.MapGet("/userName",[Microsoft.AspNetCore.Authorization.Authorize] async(HttpContext http , AppDbContext db)=>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    int userId = int.Parse(userIdStr);
    var user = await db.Users.FindAsync(userId);
    if (user is null)
    {
        return Results.NotFound();
    }
    return Results.Ok(new { userName = user.Username });
});

//------- // Section // --------
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

// get all Section 
app.MapGet("/section", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db) =>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    int userId = int.Parse(userIdStr);
    var sections = await db.Sections.Where(section => section.UserId == userId).ToListAsync();
    return Results.Ok(sections);
});

// update one section 
app.MapPut("/section/update/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, Section newSection, int id) =>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    var Section = await db.Sections.FindAsync(id);
    if (Section is null)
    {
        return Results.NotFound();
    }
    int userId = int.Parse(userIdStr);
    if (Section.UserId != userId)
    {
        return Results.Forbid();
    }
    Section.Name = newSection.Name;
    await db.SaveChangesAsync();
    return Results.Ok(Section);
});
// delete Section 
app.MapDelete("/section/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, int id) =>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }

    int userId = int.Parse(userIdStr);

    var section = await db.Sections.Include(s => s.ToDoTasks).FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

    if (section is null)
    {
        return Results.NotFound("Section not found or not yours.");
    }

    db.ToDoTasks.RemoveRange(section.ToDoTasks);
    db.Sections.Remove(section);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = $"Section {id} and {section.ToDoTasks.Count} todos deleted successfully." });
});


//------- // ToDo // --------
// add new toDo
app.MapPost("/todo/{sectionId:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, ToDoTask newTodo, int sectionId) =>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    var section = await db.Sections.FindAsync(sectionId);
    if (section is null)
    {
        return Results.NotFound();
    }
    int userId = int.Parse(userIdStr);
    if (section.UserId != userId)
    {
        return Results.Forbid();
    }
    
    newTodo.SectionId = sectionId;
    newTodo.CreateDate = DateTime.Now;
    newTodo.completed = false;
    db.ToDoTasks.Add(newTodo);
    await db.SaveChangesAsync();
    return Results.Ok("تمت اضافة الاضافة");

});

// get all toDo 
app.MapGet("/todo", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db) =>
{

    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    int userId = int.Parse(userIdStr);
    var sections = await db.Sections.Where(s => s.UserId == userId).Include(s => s.ToDoTasks).ToListAsync();
    var toDos = sections.SelectMany(s => s.ToDoTasks).Select(t => new {t.Id,t.Name,t.Content,t.SectionId,t.completed,t.CreateDate,SectionName=t.Section.Name}).ToList();
    return Results.Ok(toDos);

 
});

// get one Section's todos 
app.MapGet("/section/{id:int}/todos", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, int id) =>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    int userId = int.Parse(userIdStr);
    var section = await db.Sections.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
    if (section is null)
    {
        return Results.NotFound();
    }
    var toDos = await db.ToDoTasks.Where(todo => todo.SectionId == id).Select(t=> new{t.Id,t.Name,t.SectionId,t.Content,t.completed,t.CreateDate,SectionName=t.Section.Name}).ToListAsync();
    return Results.Ok(toDos);


});
// update one toDo
app.MapPut("/todo/update/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, ToDoTask newTodo, int id) =>
{
     var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    int userId = int.Parse(userIdStr);

   
    var toDo = await db.ToDoTasks
        .Include(t => t.Section)
        .FirstOrDefaultAsync(t => t.Id == id && t.Section.UserId == userId);

    if (toDo is null)
    {
        return Results.NotFound("Todo not found or not yours");
    }
    toDo.Name = newTodo.Name;
    toDo.Content = newTodo.Content;
    await db.SaveChangesAsync();
    return Results.Ok(new{toDo.Id,toDo.Name,toDo.Content});

});

// completed 
app.MapPut("/todo/completed/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, int id) =>
  {
  var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    int userId = int.Parse(userIdStr);


    var toDo = await db.ToDoTasks
        .Include(t => t.Section)
        .FirstOrDefaultAsync(t => t.Id == id && t.Section.UserId == userId);

    if (toDo is null)
    {
        return Results.NotFound("Todo not found or not yours");
    }
      toDo.completed = !toDo.completed;

      await db.SaveChangesAsync();
      return Results.Ok(new{toDo.Id,toDo.Name,toDo.Content,toDo.completed});

  });

// delete toDo 
app.MapDelete("/todo/{id:int}", [Microsoft.AspNetCore.Authorization.Authorize] async (HttpContext http, AppDbContext db, int id) =>
{
    var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdStr is null)
    {
        return Results.Unauthorized();
    }
    int userId = int.Parse(userIdStr);


    var toDo = await db.ToDoTasks
        .Include(t => t.Section)
        .FirstOrDefaultAsync(t => t.Id == id && t.Section.UserId == userId);

    if (toDo is null)
    {
        return Results.NotFound("Todo not found or not yours");
    }
    db.ToDoTasks.Remove(toDo);
    await db.SaveChangesAsync();
    return Results.Ok($"{id}  deleted!!");
});


app.Run();
