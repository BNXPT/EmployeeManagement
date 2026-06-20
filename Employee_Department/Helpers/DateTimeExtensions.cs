using System.Globalization;

namespace Employee_Department.Helpers
{
    public static class DateTimeExtensions
    {
        private static readonly CultureInfo ThaiCulture = CreateThaiCulture();

        private static CultureInfo CreateThaiCulture()
        {
            var c = new CultureInfo("th-TH");
            c.DateTimeFormat.Calendar = new ThaiBuddhistCalendar();
            return c;
        }

        // แสดงวันที่แบบ พ.ศ.
        public static string ToThaiDate(this DateTime dt, string format = "dd/MM/yyyy")
            => dt.ToString(format, ThaiCulture);

        // วันที่ + เวลา 
        public static string ToThaiDateTime(this DateTime dt, string format = "dd/MM/yyyy HH:mm")
            => dt.ToString(format, ThaiCulture);

        // รองรับ nullable — คืน "-" ถ้า null
        public static string ToThaiDate(this DateTime? dt, string format = "dd/MM/yyyy")
            => dt.HasValue ? dt.Value.ToString(format, ThaiCulture) : "-";

        public static string ToThaiDateTime(this DateTime? dt, string format = "dd/MM/yyyy HH:mm")
            => dt.HasValue ? dt.Value.ToString(format, ThaiCulture) : "-";

        // แบบยาว 
        public static string ToThaiLongDate(this DateTime dt)
            => dt.ToString("d MMMM yyyy", ThaiCulture);
    }
}