# Implementasi Class Diagram — WPF

Proyek aplikasi desktop WPF C# ini menerapkan class diagram tugas Modul 3. Setiap atribut UML
dibuat sebagai instance variable `private` dan dibungkus oleh property `public`, sesuai contoh `Employee`
pada modul. `App.xaml` mengatur startup aplikasi, `Views/MainWindow.xaml` mendefinisikan antarmuka dengan
XAML, dan seluruh class hasil class diagram ditempatkan pada folder `Models`.

## Pemetaan UML ke C#

- `string`, `int`, dan `double` dipetakan langsung ke tipe C#.
- `date` dipetakan menjadi `DateTime`.
- `Evaluasi[]` dipetakan menjadi array `Evaluasi[]`.
- `File` dipetakan menjadi `System.IO.FileInfo`, sebab `System.IO.File` adalah static class.
- Multiplicity `*` dipetakan ke `List<T>` yang diekspos sebagai `IReadOnlyList<T>`.
- Multiplicity `0..1` pada `SimulasiEmisi` dipetakan menjadi nullable reference `SimulasiEmisi?`.
- `JenisKomponen` dan `JenisPekerjaan` diterapkan sebagai `enum`.

## Build

```bash
dotnet build ImplementasiClassDiagram.csproj
```

Project menargetkan `net8.0-windows` dan dijalankan pada sistem operasi Windows dengan .NET 8 SDK.
