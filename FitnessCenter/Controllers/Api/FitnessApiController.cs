using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers.Api
{
    // REST API controller sınıfı
    [Route("api/[controller]")]
    [ApiController]
    public class FitnessApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FitnessApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Antrenör API

        // Tüm antrenörleri listele
        [HttpGet("trainers")]
        public async Task<ActionResult<IEnumerable<object>>> GetTrainers()
        {
            var trainers = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .Where(t => t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    t.FirstName,
                    t.LastName,
                    FullName = t.FirstName + " " + t.LastName,
                    t.Email,
                    t.Phone,
                    t.Specializations,
                    t.ExperienceYears,
                    t.ProfileImageUrl,
                    WorkingHours = new
                    {
                        Start = $"{t.WorkStartTime.Hours:D2}:{t.WorkStartTime.Minutes:D2}",
                        End = $"{t.WorkEndTime.Hours:D2}:{t.WorkEndTime.Minutes:D2}"
                    },
                    Gym = new
                    {
                        t.Gym!.Id,
                        t.Gym.Name
                    },
                    Services = t.TrainerServices.Select(ts => new
                    {
                        ts.Service!.Id,
                        ts.Service.Name,
                        ts.Service.Price,
                        ts.Service.DurationMinutes
                    })
                })
                .ToListAsync();

            return Ok(trainers);
        }

        // Antrenör detayını getir
        [HttpGet("trainers/{id}")]
        public async Task<ActionResult<object>> GetTrainer(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .Include(t => t.Availabilities)
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id,
                    t.FirstName,
                    t.LastName,
                    FullName = t.FirstName + " " + t.LastName,
                    t.Email,
                    t.Phone,
                    t.Specializations,
                    t.Biography,
                    t.ExperienceYears,
                    t.ProfileImageUrl,
                    t.IsActive,
                    WorkingHours = new
                    {
                        Start = $"{t.WorkStartTime.Hours:D2}:{t.WorkStartTime.Minutes:D2}",
                        End = $"{t.WorkEndTime.Hours:D2}:{t.WorkEndTime.Minutes:D2}"
                    },
                    Gym = new
                    {
                        t.Gym!.Id,
                        t.Gym.Name,
                        t.Gym.Address
                    },
                    Services = t.TrainerServices.Select(ts => new
                    {
                        ts.Service!.Id,
                        ts.Service.Name,
                        ts.Service.Price,
                        ts.Service.DurationMinutes
                    }),
                    Availabilities = t.Availabilities.Select(a => new
                    {
                        a.DayOfWeek,
                        Start = $"{a.StartTime.Hours:D2}:{a.StartTime.Minutes:D2}",
                        End = $"{a.EndTime.Hours:D2}:{a.EndTime.Minutes:D2}",
                        a.IsAvailable
                    })
                })
                .FirstOrDefaultAsync();

            if (trainer == null)
            {
                return NotFound(new { message = "Antrenör bulunamadı" });
            }

            return Ok(trainer);
        }

        // Belirli tarihte müsait antrenörleri getir
        [HttpGet("trainers/available")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailableTrainers(
            [FromQuery] DateTime date, 
            [FromQuery] int? serviceId = null)
        {
            var dayOfWeek = date.DayOfWeek;

            var query = _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .Include(t => t.Availabilities)
                .Where(t => t.IsActive && 
                           t.Availabilities.Any(a => a.DayOfWeek == dayOfWeek && a.IsAvailable));

            // Filter by service if specified
            if (serviceId.HasValue)
            {
                query = query.Where(t => t.TrainerServices.Any(ts => ts.ServiceId == serviceId.Value));
            }

            var trainers = await query
                .Select(t => new
                {
                    t.Id,
                    t.FirstName,
                    t.LastName,
                    FullName = t.FirstName + " " + t.LastName,
                    t.Specializations,
                    t.ExperienceYears,
                    t.ProfileImageUrl,
                    Gym = t.Gym!.Name,
                    Availability = t.Availabilities
                        .Where(a => a.DayOfWeek == dayOfWeek && a.IsAvailable)
                        .Select(a => new
                        {
                            Start = $"{a.StartTime.Hours:D2}:{a.StartTime.Minutes:D2}",
                            End = $"{a.EndTime.Hours:D2}:{a.EndTime.Minutes:D2}"
                        })
                        .FirstOrDefault(),
                    Services = t.TrainerServices.Select(ts => new
                    {
                        ts.Service!.Id,
                        ts.Service.Name
                    })
                })
                .ToListAsync();

            return Ok(new
            {
                Date = date.ToString("yyyy-MM-dd"),
                DayOfWeek = dayOfWeek.ToString(),
                AvailableTrainers = trainers
            });
        }

        // Uzmanlık alanına göre antrenör ara
        [HttpGet("trainers/search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchTrainers(
            [FromQuery] string? specialization = null,
            [FromQuery] int? minExperience = null)
        {
            var query = _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .Where(t => t.IsActive);

            // Uzmanlık alanına göre filtrele
            if (!string.IsNullOrEmpty(specialization))
            {
                query = query.Where(t => t.Specializations != null && 
                                        t.Specializations.ToLower().Contains(specialization.ToLower()));
            }

            // Minimum deneyime göre filtrele
            if (minExperience.HasValue)
            {
                query = query.Where(t => t.ExperienceYears >= minExperience.Value);
            }

            var trainers = await query
                .Select(t => new
                {
                    t.Id,
                    t.FirstName,
                    t.LastName,
                    FullName = t.FirstName + " " + t.LastName,
                    t.Specializations,
                    t.ExperienceYears,
                    Gym = t.Gym!.Name
                })
                .ToListAsync();

            return Ok(trainers);
        }

        #endregion

        #region Randevu API

        // Üyenin randevularını getir
        [HttpGet("appointments/member/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetMemberAppointments(string userId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                    .ThenInclude(s => s!.Gym)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .Select(a => new
                {
                    a.Id,
                    a.AppointmentDate,
                    AppointmentTime = $"{a.AppointmentTime.Hours:D2}:{a.AppointmentTime.Minutes:D2}",
                    EndTime = $"{a.EndTime.Hours:D2}:{a.EndTime.Minutes:D2}",
                    Status = a.Status.ToString(),
                    a.TotalPrice,
                    a.Notes,
                    a.CreatedAt,
                    Trainer = new
                    {
                        a.Trainer!.Id,
                        FullName = a.Trainer.FirstName + " " + a.Trainer.LastName
                    },
                    Service = new
                    {
                        a.Service!.Id,
                        a.Service.Name,
                        a.Service.DurationMinutes
                    },
                    Gym = a.Service.Gym!.Name
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // Belirli tarihteki randevuları getir
        [HttpGet("appointments/date/{date}")]
        public async Task<ActionResult<IEnumerable<object>>> GetAppointmentsByDate(DateTime date)
        {
            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Where(a => a.AppointmentDate.Date == date.Date)
                .OrderBy(a => a.AppointmentTime)
                .Select(a => new
                {
                    a.Id,
                    AppointmentTime = $"{a.AppointmentTime.Hours:D2}:{a.AppointmentTime.Minutes:D2}",
                    EndTime = $"{a.EndTime.Hours:D2}:{a.EndTime.Minutes:D2}",
                    Status = a.Status.ToString(),
                    Member = a.User!.FirstName + " " + a.User.LastName,
                    Trainer = a.Trainer!.FirstName + " " + a.Trainer.LastName,
                    Service = a.Service!.Name,
                    a.TotalPrice
                })
                .ToListAsync();

            return Ok(new
            {
                Date = date.ToString("yyyy-MM-dd"),
                TotalAppointments = appointments.Count,
                Appointments = appointments
            });
        }

        // Antrenörün randevularını getir
        [HttpGet("appointments/trainer/{trainerId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetTrainerAppointments(
            int trainerId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Service)
                .Where(a => a.TrainerId == trainerId);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate <= endDate.Value);
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .Select(a => new
                {
                    a.Id,
                    a.AppointmentDate,
                    AppointmentTime = $"{a.AppointmentTime.Hours:D2}:{a.AppointmentTime.Minutes:D2}",
                    EndTime = $"{a.EndTime.Hours:D2}:{a.EndTime.Minutes:D2}",
                    Status = a.Status.ToString(),
                    Member = a.User!.FirstName + " " + a.User.LastName,
                    Service = a.Service!.Name,
                    a.TotalPrice
                })
                .ToListAsync();

            return Ok(appointments);
        }

        #endregion

        #region Hizmet API

        // Tüm hizmetleri listele
        [HttpGet("services")]
        public async Task<ActionResult<IEnumerable<object>>> GetServices()
        {
            var services = await _context.Services
                .Include(s => s.Gym)
                .Include(s => s.TrainerServices)
                    .ThenInclude(ts => ts.Trainer)
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.DurationMinutes,
                    s.Price,
                    s.Category,
                    s.ImageUrl,
                    Gym = s.Gym!.Name,
                    AvailableTrainers = s.TrainerServices
                        .Where(ts => ts.Trainer!.IsActive)
                        .Select(ts => new
                        {
                            ts.Trainer!.Id,
                            FullName = ts.Trainer.FirstName + " " + ts.Trainer.LastName
                        })
                })
                .ToListAsync();

            return Ok(services);
        }

        // Kategoriye göre hizmetleri filtrele
        [HttpGet("services/category/{category}")]
        public async Task<ActionResult<IEnumerable<object>>> GetServicesByCategory(string category)
        {
            var services = await _context.Services
                .Include(s => s.Gym)
                .Where(s => s.IsActive && s.Category != null && 
                           s.Category.ToLower().Contains(category.ToLower()))
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    s.DurationMinutes,
                    s.Price,
                    s.Category,
                    Gym = s.Gym!.Name
                })
                .ToListAsync();

            return Ok(services);
        }

        #endregion

        #region İstatistik API

        // Genel istatistikleri getir
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            var stats = new
            {
                TotalTrainers = await _context.Trainers.CountAsync(t => t.IsActive),
                TotalServices = await _context.Services.CountAsync(s => s.IsActive),
                TotalAppointments = await _context.Appointments.CountAsync(),
                TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate.Date == DateTime.Today),
                PendingAppointments = await _context.Appointments
                    .CountAsync(a => a.Status == AppointmentStatus.Pending),
                CompletedAppointments = await _context.Appointments
                    .CountAsync(a => a.Status == AppointmentStatus.Completed),
                TotalRevenue = await _context.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .SumAsync(a => a.TotalPrice),
                MonthlyRevenue = await _context.Appointments
                    .Where(a => a.Status == AppointmentStatus.Completed &&
                               a.AppointmentDate.Month == DateTime.Now.Month &&
                               a.AppointmentDate.Year == DateTime.Now.Year)
                    .SumAsync(a => a.TotalPrice)
            };

            return Ok(stats);
        }

        #endregion
    }
}
