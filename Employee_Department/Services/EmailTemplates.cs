using Employee_Department.Models;
using Employee_Department.Helpers;

namespace Employee_Department.Services
{
    public static class EmailTemplates
    {
        public static string BuildLeaveRequestEmail(
            LeaveRequest req,
            Employee employee,
            string departmentName,
            string approveUrl,
            string rejectUrl,
            string viewUrl)
        {
            var dateRange = req.StartDate.Date == req.EndDate.Date
                ? req.StartDate.ToThaiDate()
                : $"{req.StartDate.ToThaiDate()} - {req.EndDate.ToThaiDate()}";

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Tahoma,Arial,sans-serif;background:#f5f7fa;margin:0;padding:24px;color:#1f2937;'>
  <div style='max-width:600px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;border:1px solid #e5e7eb;'>
    <div style='background:linear-gradient(90deg,#3b82f6,#60a5fa);padding:24px;color:#fff;'>
      <h2 style='margin:0;font-size:22px;'>📋 คำขอลางานใหม่</h2>
      <p style='margin:6px 0 0;opacity:.9;'>มีพนักงานยื่นคำขอลางาน รอการพิจารณา</p>
    </div>
    <div style='padding:24px;'>
      <table style='width:100%;border-collapse:collapse;font-size:15px;'>
        <tr><td style='color:#6b7280;padding:8px 0;width:140px;'>เลขที่คำขอ</td>
            <td style='padding:8px 0;font-weight:600;'>#{req.LeaveRequestId:D5}</td></tr>
        <tr><td style='color:#6b7280;padding:8px 0;'>ชื่อพนักงาน</td>
            <td style='padding:8px 0;font-weight:600;'>{employee.FullName}</td></tr>
        <tr><td style='color:#6b7280;padding:8px 0;'>แผนก</td>
            <td style='padding:8px 0;'>{departmentName}</td></tr>
        <tr><td style='color:#6b7280;padding:8px 0;'>ตำแหน่ง</td>
            <td style='padding:8px 0;'>{employee.Position}</td></tr>
        <tr><td style='color:#6b7280;padding:8px 0;'>ประเภทการลา</td>
            <td style='padding:8px 0;'><span style='background:#dbeafe;color:#1d4ed8;padding:4px 12px;border-radius:12px;font-size:13px;'>{req.LeaveType}</span></td></tr>
        <tr><td style='color:#6b7280;padding:8px 0;'>ช่วงวันที่ลา</td>
            <td style='padding:8px 0;font-weight:600;'>{dateRange} <span style='color:#6b7280;font-weight:400;'>({req.DaysCount} วัน)</span></td></tr>
        <tr><td style='color:#6b7280;padding:8px 0;vertical-align:top;'>เหตุผล</td>
            <td style='padding:8px 0;'>{System.Net.WebUtility.HtmlEncode(req.Reason)}</td></tr>
        <tr><td style='color:#6b7280;padding:8px 0;'>วันที่ยื่นคำขอ</td>
            <td style='padding:8px 0;'>{req.RequestedAt:dd/MM/yyyy HH:mm}</td></tr>
      </table>

      <div style='margin-top:32px;text-align:center;'>
        <p style='color:#6b7280;font-size:14px;margin-bottom:16px;'>กรุณาคลิกปุ่มด้านล่างเพื่อดำเนินการ</p>
        <a href='{approveUrl}' style='display:inline-block;background:#16a34a;color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;margin:6px;'>
          ✓ อนุมัติคำขอ
        </a>
        <a href='{rejectUrl}' style='display:inline-block;background:#dc2626;color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;margin:6px;'>
          ✗ ปฏิเสธคำขอ
        </a>
      </div>

      <div style='margin-top:20px;text-align:center;'>
        <a href='{viewUrl}' style='color:#3b82f6;font-size:14px;text-decoration:none;'>ดูรายละเอียดในระบบ →</a>
      </div>
    </div>
    <div style='background:#f9fafb;padding:16px;text-align:center;color:#9ca3af;font-size:12px;border-top:1px solid #e5e7eb;'>
      ระบบจัดการแผนกพนักงาน — อีเมลนี้สร้างโดยระบบอัตโนมัติ
    </div>
  </div>
</body>
</html>";
        }

        public static string BuildEmployeeNotificationEmail(LeaveRequest req, Employee employee, bool isApproved, string? comment)
        {
            var statusColor = isApproved ? "#16a34a" : "#dc2626";
            var statusBg = isApproved ? "#dcfce7" : "#fee2e2";
            var statusText = isApproved ? "✓ อนุมัติ" : "✗ ปฏิเสธ";
            var dateRange = req.StartDate.Date == req.EndDate.Date
                ? req.StartDate.ToString("dd/MM/yyyy")
                : $"{req.StartDate:dd/MM/yyyy} - {req.EndDate:dd/MM/yyyy}";

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Tahoma,Arial,sans-serif;background:#f5f7fa;margin:0;padding:24px;color:#1f2937;'>
  <div style='max-width:600px;margin:0 auto;background:#fff;border-radius:12px;overflow:hidden;border:1px solid #e5e7eb;'>
    <div style='background:{statusColor};padding:24px;color:#fff;text-align:center;'>
      <h2 style='margin:0;font-size:22px;'>ผลการพิจารณาคำขอลางาน</h2>
      <div style='display:inline-block;margin-top:12px;background:#fff;color:{statusColor};padding:8px 20px;border-radius:20px;font-weight:600;'>{statusText}</div>
    </div>
    <div style='padding:24px;'>
      <p style='font-size:16px;'>เรียน คุณ {employee.FullName}</p>
      <p>คำขอลางานของคุณ ได้รับการพิจารณาแล้ว</p>
      <table style='width:100%;border-collapse:collapse;margin-top:16px;font-size:15px;'>
        <tr><td style='color:#6b7280;padding:6px 0;width:140px;'>เลขที่คำขอ</td><td style='font-weight:600;'>#{req.LeaveRequestId:D5}</td></tr>
        <tr><td style='color:#6b7280;padding:6px 0;'>ประเภท</td><td>{req.LeaveType}</td></tr>
        <tr><td style='color:#6b7280;padding:6px 0;'>วันที่</td><td>{dateRange} ({req.DaysCount} วัน)</td></tr>
      </table>
      {(string.IsNullOrWhiteSpace(comment) ? "" : $@"<div style='margin-top:16px;padding:12px;background:{statusBg};border-radius:8px;'><strong>ความเห็น:</strong> {System.Net.WebUtility.HtmlEncode(comment)}</div>")}
    </div>
    <div style='background:#f9fafb;padding:16px;text-align:center;color:#9ca3af;font-size:12px;border-top:1px solid #e5e7eb;'>
      ระบบจัดการแผนกพนักงาน
    </div>
  </div>
</body>
</html>";
        }
    }
}
