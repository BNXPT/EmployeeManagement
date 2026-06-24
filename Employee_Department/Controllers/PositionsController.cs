using Employee_Department.Models;
using Employee_Department.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Department.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PositionsController : Controller
    {
        private readonly IPositionService _service;
        public PositionsController(IPositionService service) => _service = service;

        public async Task<IActionResult> Index()
        {
            ViewBag.PosData = await _service.GetAllWithUsageAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
            => Json(new { success = true, data = await _service.GetAllWithUsageAsync() });

        [HttpGet]
        public async Task<IActionResult> GetRows(string? search)
        {
            var data = await _service.GetAllWithUsageAsync(search);
            return PartialView("_PositionRows", data);
        }

        [HttpGet]
        public async Task<IActionResult> GetOne(int id)
        {
            var p = await _service.GetByIdAsync(id);
            return p == null
                ? Json(new { success = false, message = "ไม่พบ" })
                : Json(new { success = true, data = new { p.PositionId, p.PositionName } });
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("PositionName")] Position position)
        {
            var result = await _service.CreateAsync(position);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("PositionId,PositionName")] Position position)
        {
            if (id != position.PositionId) return Json(new { success = false, message = "ID ไม่ตรง" });
            var result = await _service.UpdateAsync(position);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}