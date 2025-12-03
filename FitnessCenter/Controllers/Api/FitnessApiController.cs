using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class FitnessApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FitnessApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Trainers API

        /// <summary>
        /// Tüm antrenörleri listeler
        /// GET: api/FitnessApi/trainers
        /// </summary>
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
                        Start = t.WorkStartTime.ToString(@"hh\:mm"),
                        End = t.WorkEndTime.ToString(@"hh\:mm")
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

        /// <summary>
        /// ID'ye göre antrenör detayı getirir
        /// GET: api/FitnessApi/trainers/5
        /// </summary>
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
                        Start = t.WorkStartTime.ToString(@"hh\:mm"),
                        End = t.WorkEndTime.ToString(@"hh\:mm")
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
                        Start = a.StartTime.ToString(@"hh\:mm"),
                        End = a.EndTime.ToString(@"hh\:mm"),
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

        /// <summary>
        /// Belirli bir tarihte uygun antrenörleri getirir
        /// GET: api/FitnessApi/trainers/available?date=2024-01-15&serviceId=1
        /// </summary>
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
                            Start = a.StartTime.ToString(@"hh\:mm"),
                            End = a.EndTime.ToString(@"hh\:mm")
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

        /// <summary>
        /// Uzmanlık alanına göre antrenörleri filtreler
        /// GET: api/FitnessApi/trainers/search?specialization=yoga
        /// </summary>
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

            // Filter by specialization
            if (!string.IsNullOrEmpty(specialization))
            {
                query = query.Where(t => t.Specializations != null && 
                                        t.Specializations.ToLower().Contains(specialization.ToLower()));
            }

            // Filter by minimum experience
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

        #region Appointments API

        /// <summary>
        /// Üyenin randevularını getirir
        /// GET: api/FitnessApi/appointments/member/{userId}
        /// </summary>
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
                    AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm"),
                    EndTime = a.EndTime.ToString(@"hh\:mm"),
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

        /// <summary>
        /// Belirli bir tarihteki randevuları getirir
        /// GET: api/FitnessApi/appointments/date/2024-01-15
        /// </summary>
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
                    AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm"),
                    EndTime = a.EndTime.ToString(@"hh\:mm"),
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

        /// <summary>
        /// Antrenörün randevularını getirir
        /// GET: api/FitnessApi/appointments/trainer/5?startDate=2024-01-01&endDate=2024-01-31
        /// </summary>
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
                    AppointmentTime = a.AppointmentTime.ToString(@"hh\:mm"),
                    EndTime = a.EndTime.ToString(@"hh\:mm"),
                    Status = a.Status.ToString(),
                    Member = a.User!.FirstName + " " + a.User.LastName,
                    Service = a.Service!.Name,
                    a.TotalPrice
                })
                .ToListAsync();

            return Ok(appointments);
        }

        #endregion

        #region Services API

        /// <summary>
        /// Tüm hizmetleri listeler
        /// GET: api/FitnessApi/services
        /// </summary>
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

        /// <summary>
        /// Kategoriye göre hizmetleri filtreler
        /// GET: api/FitnessApi/services/category/yoga
        /// </summary>
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

        #region Stats API

        /// <summary>
        /// Genel istatistikleri getirir
        /// GET: api/FitnessApi/stats
        /// </summary>
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
