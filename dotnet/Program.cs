// Program.cs
using System;
using System.Collections.Generic;
using System.Linq;

enum Shift
{
    Morning = 0,
    Afternoon = 1,
    Evening = 2,
    None = 3
}

class Employee
{
    public string Name { get; }

    // preferred shift for each day (0=Sunday .. 6=Saturday)
    public Shift[] PreferredPerDay = new Shift[7];

    // global ranking: first = highest preference (contains all 3 shifts in order)
    public Shift[] GlobalRanking = new Shift[3];
    public int AssignedDays = 0;
    public Employee(string name)
    {
        Name = name;
        for (int i = 0; i < 7; i++)
        {
            PreferredPerDay[i] = Shift.None;
            GlobalRanking = new[]
            {
                Shift.Morning,
                Shift.Afternoon,
                Shift.Evening
            };
        }
    }
}

class Program
{
    static Random rnd = new Random();
    static string[] dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
    static string ShiftName(Shift s) => s == Shift.Morning ? "M" : s == Shift.Afternoon ? "A" : s == Shift.Evening ? "E" : "-";

    static void Main()
    {
        Console.WriteLine("Employee Scheduler — C# console\n");

        List<Employee> employees;
        Console.Write("Use sample data? (y/n): ");

        if (Console.ReadLine().Trim().ToLower() == "y")
        {
            employees = SampleEmployees();
        }
        else
        {
            employees = InputEmployees();
        }

        // schedule: day -> shift -> list of employees
        var schedule = new List<List<List<Employee>>>();
        for (int d = 0; d < 7; d++)
        {
            schedule.Add(new List<List<Employee>> { new List<Employee>(), new List<Employee>(), new List<Employee>() });
        }

        // First pass: try to assign employees to their preferred shift each day if possible
        foreach (var emp in employees)
        {
            for (int day = 0; day < 7; day++)
            {
                if (emp.AssignedDays >= 5)
                {
                    break;
                }
                var pref = emp.PreferredPerDay[day];

                if (pref == Shift.None)
                {
                    continue;
                }

                // no per-shift cap, only company minimum required
                if (!IsAssigned(emp, schedule, day) && schedule[day][(int)pref].Count < 100)
                {
                    // assign tentatively if preferred not too full (we'll enforce min later)
                    Assign(emp, schedule, day, pref);
                }
            }
        }

        // Resolve conflicts: if a preferred shift on a day is "full" — defined here as already assigned to same employee? 
        // (We prevented >1 shift/day). Instead we ensure coverage: for any employee with preference not assigned, try to put them other shift same day or next day.
        foreach (var emp in employees)
        {
            for (int day = 0; day < 7; day++)
            {
                if (emp.AssignedDays >= 5)
                {
                    break;
                }

                var pref = emp.PreferredPerDay[day];
                if (pref == Shift.None)
                {
                    continue;
                }

                if (!IsAssigned(emp, schedule, day))
                {
                    // Try other shifts same day using global ranking preference order
                    var tried = new HashSet<Shift>();
                    foreach (var s in emp.GlobalRanking)
                    {
                        if (tried.Contains(s)) continue;
                        tried.Add(s);
                        if (!IsAssigned(emp, schedule, day) && !IsEmployeeAssignedInShift(emp, schedule, day) && !schedule[day][(int)s].Contains(emp))
                        {
                            if (!IsAssignedThisDay(emp, schedule, day))
                            {
                                Assign(emp, schedule, day, s);
                                break;
                            }
                        }
                    }
                    // if still not assigned, try next day
                    if (!IsAssigned(emp, schedule, day))
                    {
                        int next = day + 1;
                        if (next < 7)
                        {
                            foreach (var s in emp.GlobalRanking)
                            {
                                if (emp.AssignedDays >= 5) break;
                                if (!schedule[next][(int)s].Contains(emp) && !IsAssignedThisDay(emp, schedule, next))
                                {
                                    Assign(emp, schedule, next, s);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Ensure at least 2 employees per shift per day. If fewer, randomly assign additional employees who have not worked 5 days yet and are free that day.
        for (int d = 0; d < 7; d++)
        {
            for (int s = 0; s < 3; s++)
            {
                while (schedule[d][s].Count < 2)
                {
                    var pool = employees.Where(e => e.AssignedDays < 5 && !IsAssignedThisDay(e, schedule, d)).ToList();

                    if (pool.Count == 0)
                    {
                        // cannot satisfy further
                        break;
                    }
                    var pick = pool[rnd.Next(pool.Count)];
                    Assign(pick, schedule, d, (Shift)s);
                }
            }
        }

        // Final output
        PrintSchedule(schedule);

        Console.WriteLine("\nDone. Press Enter to exit.");
        Console.ReadLine();
    }

    static void Assign(Employee e, List<List<List<Employee>>> schedule, int day, Shift shift)
    {
        // protect: do not assign if assigned this day or assigned >=5 days
        if (IsAssignedThisDay(e, schedule, day))
        {
            return;
        }
        if (e.AssignedDays >= 5)
        {
            return;
        }
        schedule[day][(int)shift].Add(e);
        e.AssignedDays++;
    }

    static bool IsAssigned(Employee e, List<List<List<Employee>>> schedule, int day)
    {
        // assigned any shift on day
        return schedule[day].Any(list => list.Contains(e));
    }
    static bool IsAssignedThisDay(Employee e, List<List<List<Employee>>> schedule, int day) => IsAssigned(e, schedule, day);
    static bool IsEmployeeAssignedInShift(Employee e, List<List<List<Employee>>> schedule, int day) => schedule[day].Any(list => list.Contains(e));

    static void PrintSchedule(List<List<List<Employee>>> schedule)
    {
        Console.WriteLine("\nFinal weekly schedule (Shift lists are 'Name1,Name2'):\n");
        Console.Write("Day\tShift\tEmployees\n");
        Console.WriteLine("--------------------------------------");
        for (int d = 0; d < 7; d++)
        {
            for (int s = 0; s < 3; s++)
            {
                var names = schedule[d][s].Select(x => x.Name).ToArray();
                Console.WriteLine($"{dayNames[d]}\t{ShiftName((Shift)s)}\t{(names.Length == 0 ? "-" : string.Join(",", names))}");
            }
        }
        Console.WriteLine("\nEmployee totals (days assigned):");
        // To print employees sorted
        var allEmps = schedule.SelectMany(day => day.SelectMany(shift => shift)).Distinct();
        var empList = allEmps.ToList();
        // also include those with 0 to 5
        foreach (var e in empList.OrderBy(e => e.Name))
        {
            Console.WriteLine($"{e.Name}: {e.AssignedDays} days");
        }
    }

    static List<Employee> SampleEmployees()
    {
        // sample: 8 employees with preferences
        var list = new List<Employee>
        {
            new Employee("Alan"),
            new Employee("Bob"),
            new Employee("Carol"),
            new Employee("Dan"),
            new Employee("Eve"),
            new Employee("Frank"),
            new Employee("Grace"),
            new Employee("Heidi"),
        };
        // set per-day preferences (simple)

        // Alice prefers mornings all week
        foreach (int d in Enumerable.Range(0, 7)) list[0].PreferredPerDay[d] = Shift.Morning;

        // Bob prefers afternoons
        foreach (int d in Enumerable.Range(0, 7)) list[1].PreferredPerDay[d] = Shift.Afternoon;

        // Carol prefers evenings on weekdays, morning weekends
        for (int d = 0; d < 7; d++) list[2].PreferredPerDay[d] = (d <= 4) ? Shift.Evening : Shift.Morning;

        // Dan mixed
        list[3].PreferredPerDay = new[] { Shift.Morning, Shift.Afternoon, Shift.Evening, Shift.Morning, Shift.Afternoon, Shift.Evening, Shift.Morning };

        // Eve, Frank, Grace, Heidi random-ish
        list[4].PreferredPerDay = new[] { Shift.Afternoon, Shift.Afternoon, Shift.Morning, Shift.Morning, Shift.Evening, Shift.Afternoon, Shift.Evening };
        list[5].PreferredPerDay = new[] { Shift.Morning, Shift.Morning, Shift.Morning, Shift.Morning, Shift.None, Shift.None, Shift.None };
        list[6].PreferredPerDay = new[] { Shift.Evening, Shift.Evening, Shift.Evening, Shift.Evening, Shift.Evening, Shift.Evening, Shift.Evening };
        list[7].PreferredPerDay = new[] { Shift.None, Shift.Afternoon, Shift.Afternoon, Shift.Afternoon, Shift.None, Shift.Morning, Shift.Morning };

        // some global rankings (bonus)
        list[0].GlobalRanking = new[] { Shift.Morning, Shift.Afternoon, Shift.Evening };
        list[1].GlobalRanking = new[] { Shift.Afternoon, Shift.Evening, Shift.Morning };
        list[2].GlobalRanking = new[] { Shift.Evening, Shift.Morning, Shift.Afternoon };
        // others default
        return list;
    }

    static List<Employee> InputEmployees()
    {
        var employees = new List<Employee>();
        Console.Write("How many employees? ");
        if (!int.TryParse(Console.ReadLine(), out int n) || n <= 0)
        {
            n = 0;
        }
        for (int i = 0; i < n; i++)
        {
            Console.Write($"\nEmployee {i + 1} name: ");
            var name = Console.ReadLine().Trim();
            var e = new Employee(name);
            Console.WriteLine("Enter preferred shift for each day (m/a/e) or (morning/afternoon/evening) or '-' for no preference.");
            for (int d = 0; d < 7; d++)
            {
                Console.Write($"{dayNames[d]}: ");
                var s = Console.ReadLine().Trim().ToLower();
                if (s == "m" || s == "morning")
                {
                    e.PreferredPerDay[d] = Shift.Morning;
                }
                else if (s == "a" || s == "afternoon")
                {
                    e.PreferredPerDay[d] = Shift.Afternoon;
                }
                else if (s == "e" || s == "evening")
                {
                    e.PreferredPerDay[d] = Shift.Evening;
                }
                else
                {
                    e.PreferredPerDay[d] = Shift.None;
                }
            }
            Console.WriteLine("Enter global ranking as comma-separated from highest to lowest (options: morning,afternoon,evening). Example: morning,evening,afternoon");
            var ranking = Console.ReadLine().Trim();
            if (!string.IsNullOrEmpty(ranking))
            {
                var parts = ranking.Split(',').Select(p => p.Trim().ToLower()).Where(x => x != "").ToArray();
                var rankingList = new List<Shift>();
                foreach (var p in parts)
                {
                    if (p.StartsWith("m"))
                    {
                        rankingList.Add(Shift.Morning);
                    }
                    else if (p.StartsWith("a"))
                    {
                        rankingList.Add(Shift.Afternoon);
                    }
                    else if (p.StartsWith("e"))
                    {
                        rankingList.Add(Shift.Evening);
                    }
                }
                
                if (rankingList.Count == 3)
                {
                    e.GlobalRanking = rankingList.ToArray();
                }
            }
            employees.Add(e);
        }
        return employees;
    }
}
