using Employee_Department.Models;

namespace Employee_Department.Data
{
    public static class DbSeeder
    {
        private static readonly string[] DepartmentNames = new[]
        {
            "ฝ่ายบุคคล", "ฝ่ายบัญชี", "ฝ่ายการตลาด", "ฝ่ายไอที", "ฝ่ายขาย",
            "ฝ่ายผลิต", "ฝ่ายจัดซื้อ", "ฝ่ายคลังสินค้า", "ฝ่ายวิจัยและพัฒนา", "ฝ่ายกฎหมาย"
        };

        private static readonly string[] ManagerFullNames = new[]
        {
            "นายสมชาย ใจดี", "นางสาวสุดา รักงาน", "นายวิชัย ขยันมาก", "นางสาวพรทิพย์ สุขใจ",
            "นายธนากร มั่งมี", "นางสาวอรอนงค์ ดีงาม", "นายประยุทธ์ พัฒนา", "นางสาวมณีรัตน์ เพชรงาม",
            "นายอนุชา สร้างสรรค์", "นางสาวกัลยา ยุติธรรม"
        };

        private static readonly string[] FirstNamesMale = new[]
        {
            "จักรพงษ์", "ปรียา", "รัตนา", "วิลาวัลย์", "ณรงค์", "นพดล", "สุรชัย", "ธวัชชัย",
            "พิทักษ์", "อนันต์", "วีระ", "เกรียงไกร", "ธีระ", "สมศักดิ์", "ประเสริฐ"
        };

        private static readonly string[] FirstNamesFemale = new[]
        {
            "ปรียา", "รัตนา", "วิลาวัลย์", "สุดารัตน์", "อรพิน", "นพมาศ", "วาสนา", "ปิยะนุช",
            "จันทร์เพ็ญ", "สุภาพร", "ขวัญใจ", "พิมพ์ใจ"
        };

        private static readonly string[] LastNames = new[]
        {
            "ทองดี", "เจริญรัตน์", "ทรงศักดิ์", "อยู่ดี", "สวัสดี", "ศรีสุข", "บุญมี", "สุวรรณ",
            "มีสุข", "ใจกล้า", "แสงทอง", "พรหมจรรย์", "นพคุณ", "วงศ์ไพศาล", "อรุณรัตน์"
        };

        private static readonly string[] Positions = new[]
        {
            "เจ้าหน้าที่", "เจ้าหน้าที่ปฏิบัติการ", "ผู้ช่วย", "นักวิเคราะห์",
            "พนักงาน", "เจ้าหน้าที่อาวุโส"
        };

        public static void Seed(ApplicationDbContext context)
        {
            //Seed Positions
            if (!context.Positions.Any())
            {
                foreach (var name in Positions)
                {
                    context.Positions.Add(new Position { PositionName = name });
                }
                context.SaveChanges();
            }

            // Seed Departments
            if (!context.Departments.Any())
            {
                foreach (var name in DepartmentNames)
                {
                    context.Departments.Add(new Department { DepartmentName = name });
                }
                context.SaveChanges();
            }

            var departments = context.Departments.OrderBy(d => d.DepartmentId).ToList();

            // Seed Managers (1 per department)
            if (!context.Managers.Any())
            {
                long baseNationalId = 1100000000000L;
                for (int i = 0; i < 10 && i < departments.Count; i++)
                {
                    var nationalId = (baseNationalId + (i * 7919)).ToString().PadLeft(13, '0');
                    if (nationalId.Length > 13) nationalId = nationalId.Substring(0, 13);

                    context.Managers.Add(new Manager
                    {
                        FullName = ManagerFullNames[i],
                        NationalId = nationalId,
                        DepartmentId = departments[i].DepartmentId,
                        PhoneNumber = $"08{i}1234567",
                        Email = $"manager{i + 1}@company.com"
                    });
                }
                context.SaveChanges();
            }

            // Seed Employees (10 per department = 100)
            if (!context.Employees.Any())
            {
                var rng = new Random(42);
                long empBaseId = 2100000000000L;
                int counter = 0;
                foreach (var dept in departments)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var useFemale = rng.Next(2) == 0;
                        var firstName = useFemale
                            ? FirstNamesFemale[rng.Next(FirstNamesFemale.Length)]
                            : FirstNamesMale[rng.Next(FirstNamesMale.Length)];
                        var lastName = LastNames[rng.Next(LastNames.Length)];
                        var position = Positions[rng.Next(Positions.Length)];

                        var nationalId = (empBaseId + counter).ToString().PadLeft(13, '0');
                        if (nationalId.Length > 13) nationalId = nationalId.Substring(0, 13);

                        var phone = $"09{rng.Next(10, 99)}{rng.Next(100000, 999999)}";
                        var email = $"emp{counter + 1}@company.com";

                        context.Employees.Add(new Employee
                        {
                            FullName = $"{firstName} {lastName}",
                            NationalId = nationalId,
                            DepartmentId = dept.DepartmentId,
                            Position = position,
                            PhoneNumber = phone,
                            Email = email
                        });
                        counter++;
                    }
                }
                context.SaveChanges();
            }

            // Seed Users (Admin + Managers + Employees)
            if (!context.Users.Any())
            {
                // Admin
                context.Users.Add(new User
                {
                    Username = "admin",
                    Password = "admin123",
                    FullName = "ผู้ดูแลระบบ",
                    Role = "Admin"
                });

                // Managers
                var managers = context.Managers.OrderBy(m => m.ManagerId).ToList();
                for (int i = 0; i < managers.Count; i++)
                {
                    context.Users.Add(new User
                    {
                        Username = $"manager{i + 1}",
                        Password = $"manager{i + 1}",
                        FullName = managers[i].FullName,
                        Role = "Manager",
                        ManagerId = managers[i].ManagerId
                    });
                }

                // Employees
                var employees = context.Employees.OrderBy(e => e.EmployeeId).ToList();
                for (int i = 0; i < employees.Count; i++)
                {
                    context.Users.Add(new User
                    {
                        Username = $"emp{i + 1}",
                        Password = $"emp{i + 1}",
                        FullName = employees[i].FullName,
                        Role = "Employee",
                        EmployeeId = employees[i].EmployeeId
                    });
                }


                context.SaveChanges();
            }
        }
    }
}
