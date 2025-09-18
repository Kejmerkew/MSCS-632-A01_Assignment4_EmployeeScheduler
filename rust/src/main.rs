// src/main.rs
use rand::seq::SliceRandom;
use rand::thread_rng;
use std::collections::HashSet;
use std::io::{self, Write};

#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
enum Shift { Morning, Afternoon, Evening, None }

impl Shift {
    fn as_str(&self) -> &'static str {
        match self {
            Shift::Morning => "M",
            Shift::Afternoon => "A",
            Shift::Evening => "E",
            Shift::None => "-",
        }
    }
    fn from_str(s: &str) -> Shift {
        let x = s.trim().to_lowercase();
        if x.starts_with("m") { Shift::Morning }
        else if x.starts_with("a") { Shift::Afternoon }
        else if x.starts_with("e") { Shift::Evening }
        else { Shift::None }
    }
}

struct Employee {
    name: String,
    preferred_per_day: [Shift; 7],
    global_ranking: [Shift; 3],
    assigned_days: usize,
}

impl Employee {
    fn new(name: &str) -> Self {
        Employee {
            name: name.to_string(),
            preferred_per_day: [Shift::None; 7],
            global_ranking: [Shift::Morning, Shift::Afternoon, Shift::Evening],
            assigned_days: 0,
        }
    }
}

fn main() {
    println!("Employee Scheduler — Rust console\n");

    let mut employees = Vec::new();
    println!("Use sample data? (y/n): ");
    let mut choice = String::new();
    io::stdin().read_line(&mut choice).unwrap();
    if choice.trim().to_lowercase() == "y" {
        employees = sample_employees();
    } else {
        employees = input_employees();
    }

    // schedule[day][shift] => Vec<index-of-employee>
    let mut schedule: Vec<Vec<Vec<usize>>> = vec![vec![Vec::new(), Vec::new(), Vec::new()]; 7];

    // first pass: assign to preferred shifts if possible
    for (idx, emp) in employees.iter_mut().enumerate() {
        for day in 0..7 {
            if emp.assigned_days >= 5 { break; }
            let pref = emp.preferred_per_day[day];
            if pref == Shift::None { continue; }
            if !is_assigned_this_day(idx, &schedule, day) {
                schedule[day][shift_to_index(pref)].push(idx);
                emp.assigned_days += 1;
            }
        }
    }

    // attempt to resolve unassigned preferences by trying global ranking same day or next day
    for (idx, emp) in employees.iter_mut().enumerate() {
        for day in 0..7 {
            if emp.assigned_days >= 5 { break; }
            let pref = emp.preferred_per_day[day];
            if pref==Shift::None { continue; }
            if !is_assigned_this_day(idx, &schedule, day) {
                // try global ranking same day
                for &s in &emp.global_ranking {
                    if emp.assigned_days >= 5 { break; }
                    if !is_assigned_this_day(idx, &schedule, day) {
                        schedule[day][shift_to_index(s)].push(idx);
                        emp.assigned_days += 1;
                        break;
                    }
                }
                // else try next day
                if !is_assigned_this_day(idx, &schedule, day) && day+1 < 7 {
                    for &s in &emp.global_ranking {
                        if emp.assigned_days >= 5 { break; }
                        if !is_assigned_this_day(idx, &schedule, day+1) {
                            schedule[day+1][shift_to_index(s)].push(idx);
                            emp.assigned_days += 1;
                            break;
                        }
                    }
                }
            }
        }
    }

    // Ensure at least 2 employees per shift per day; pick random eligible employees
    for day in 0..7 {
        for s in 0..3 {
            while schedule[day][s].len() < 2 {
                let mut pool: Vec<usize> = employees.iter().enumerate()
                    .filter(|(i,e)| e.assigned_days < 5 && !is_assigned_this_day(*i, &schedule, day))
                    .map(|(i,_)| i).collect();
                if pool.is_empty() { break; }
                let mut rng = thread_rng();
                pool.shuffle(&mut rng);
                let pick = pool[0];
                schedule[day][s].push(pick);
                employees[pick].assigned_days += 1;
            }
        }
    }

    print_schedule(&schedule, &employees);
    println!("\nDone.");
}

fn shift_to_index(s: Shift) -> usize {
    match s {
        Shift::Morning => 0,
        Shift::Afternoon => 1,
        Shift::Evening => 2,
        Shift::None => 3,
    }
}

fn is_assigned_this_day(emp_idx: usize, schedule: &Vec<Vec<Vec<usize>>>, day: usize) -> bool {
    schedule[day].iter().any(|vec| vec.contains(&emp_idx))
}

fn print_schedule(schedule: &Vec<Vec<Vec<usize>>>, employees: &Vec<Employee>) {
    let day_names = ["Sun","Mon","Tue","Wed","Thu","Fri","Sat"];
    println!("\nFinal weekly schedule (Shift lists are 'Name1,Name2'):\n");
    println!("Day\tShift\tEmployees");
    println!("--------------------------------------");
    for day in 0..7 {
        for s in 0..3 {
            let names: Vec<String> = schedule[day][s].iter().map(|&idx| employees[idx].name.clone()).collect();
            let names_s = if names.is_empty() { "-".to_string() } else { names.join(",") };
            println!("{}\t{}\t{}", day_names[day], match s {0=>"M",1=>"A",2=>"E", _=>"?"}, names_s);
        }
    }
    println!("\nEmployee totals (days assigned):");
    let mut printed = HashSet::new();
    for day in 0..7 {
        for s in 0..3 {
            for &idx in &schedule[day][s] {
                printed.insert(idx);
            }
        }
    }
    let mut list: Vec<usize> = printed.into_iter().collect();
    list.sort_by_key(|&i| employees[i].name.clone());
    for idx in list {
        println!("{}: {} days", employees[idx].name, employees[idx].assigned_days);
    }
}

fn sample_employees() -> Vec<Employee> {
    let mut emps = vec![
        Employee::new("Alan"),
        Employee::new("Bob"),
        Employee::new("Carol"),
        Employee::new("Dan"),
        Employee::new("Eve"),
        Employee::new("Frank"),
        Employee::new("Grace"),
        Employee::new("Heidi"),
    ];
    for d in 0..7 { emps[0].preferred_per_day[d] = Shift::Morning; }
    for d in 0..7 { emps[1].preferred_per_day[d] = Shift::Afternoon; }
    for d in 0..7 { emps[2].preferred_per_day[d] = if d<=4 { Shift::Evening } else { Shift::Morning }; }
    emps[3].preferred_per_day = [Shift::Morning, Shift::Afternoon, Shift::Evening, Shift::Morning, Shift::Afternoon, Shift::Evening, Shift::Morning];
    emps[4].preferred_per_day = [Shift::Afternoon, Shift::Afternoon, Shift::Morning, Shift::Morning, Shift::Evening, Shift::Afternoon, Shift::Evening];
    emps[5].preferred_per_day = [Shift::Morning; 7];
    emps[6].preferred_per_day = [Shift::Evening; 7];
    emps[7].preferred_per_day = [Shift::None, Shift::Afternoon, Shift::Afternoon, Shift::Afternoon, Shift::None, Shift::Morning, Shift::Morning];
    emps[0].global_ranking = [Shift::Morning, Shift::Afternoon, Shift::Evening];
    emps[1].global_ranking = [Shift::Afternoon, Shift::Evening, Shift::Morning];
    emps[2].global_ranking = [Shift::Evening, Shift::Morning, Shift::Afternoon];
    emps
}

fn input_employees() -> Vec<Employee> {
    let mut employees: Vec<Employee> = Vec::new();
    print!("How many employees? ");
    io::stdout().flush().unwrap();
    let mut s = String::new();
    io::stdin().read_line(&mut s).unwrap();
    let n: usize = s.trim().parse().unwrap_or(0);
    for i in 0..n {
        println!("\nEmployee {} name: ", i+1);
        let mut name = String::new();
        io::stdin().read_line(&mut name).unwrap();
        let mut e = Employee::new(name.trim());
        println!("Enter preferred shift for each day (m/a/e) or '-' for no preference.");
        let day_names = ["Sun","Mon","Tue","Wed","Thu","Fri","Sat"];
        for d in 0..7 {
            print!("{}: ", day_names[d]);
            io::stdout().flush().unwrap();
            let mut s = String::new();
            io::stdin().read_line(&mut s).unwrap();
            e.preferred_per_day[d] = Shift::from_str(&s);
        }
        println!("Enter global ranking as comma-separated from highest to lowest (example: morning,evening,afternoon):");
        let mut rank = String::new();
        io::stdin().read_line(&mut rank).unwrap();
        let parts: Vec<_> = rank.split(',').map(|x| Shift::from_str(x)).collect();
        if parts.len() == 3 {
            e.global_ranking = [parts[0], parts[1], parts[2]];
        }
        employees.push(e);
    }
    employees
}
