# Employee Scheduler (C# & Rust)

This project implements an **employee scheduling application** in two different programming languages: **C# (.NET)** and **Rust**.  
It demonstrates control structures (conditionals, loops, branching) and basic scheduling logic across paradigms.

---

## 📋 Features
- Collects employee names and preferred shifts (morning, afternoon, evening) for each day.
- Enforces scheduling rules:
  - Max **1 shift per day** per employee.
  - Max **5 days per week** per employee.
  - At least **2 employees per shift** per day.
- Resolves conflicts when preferred shifts are unavailable.
- Outputs a **weekly schedule table** with employee assignments.
- Bonus: supports global **shift priority ranking** (e.g., morning > evening > afternoon).

---

## ⚙️ Prerequisites

You need to install:

- **C# / .NET SDK**
  - Download from: [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)  
  - Verify install:
    ```bash
    dotnet --version
    ```

- **Rust (with Cargo)**
  - Install via [Rustup](https://rustup.rs/) (For WSL)
    ```bash
    curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
    ```
  - Verify install:
    ```bash
    rustc --version
    cargo --version
    ```

---

## ▶️ Running the C# (.NET) Program

1. Create a new console project:
   ```bash
   dotnet new console -n DotnetEmployeeScheduler
   cd DotnetEmployeeScheduler
   ```
2. Replace the generated Program.cs with the C# code from /dotnet/Program.cs
3. Run program
   ```bash
   dotnet run
   ```
4. Enter employees manually, or use the built-in sample data by typing y.
