# Лабораторная работа 6 : внутреннее представление программы (ВПП)

## Цель

Изучить методы построения внутреннего представления программы на основе контекстно-свободной грамматики, реализовать синтаксический анализатор методом **рекурсивного спуска** и преобразовать арифметические выражения в **тетрады** и **ПОЛИЗ**.

## Грамматика

```
E → TA
A → ε | + TA | - TA
T → FB
B → ε | * FB | / FB | % FB
F → num | id | (E)
id → letter {letter | digit | _}
num → digit {digit}
```

## Реализация

| Модуль | Назначение |
|--------|------------|
| `LC1/Core/ExprScanner.cs` | Лексический анализ (числа, идентификаторы, операторы, скобки) |
| `LC1/Core/ExprRecursiveDescentParser.cs` | Синтаксический анализ, тетрады, ПОЛИЗ, вычисление |
| `LC1/MainWindow.xaml` | Графический интерфейс |

### Возможности

- Поиск **лексических** и **синтаксических** ошибок с указанием позиции в исходном тексте
- Формирование **тетрад** `(op, arg1, arg2, result)` для корректных выражений
- Преобразование в **ПОЛИЗ** (польская инверсная запись)
- **Вычисление** значения выражения (только для выражений из целых чисел, без идентификаторов)

## Запуск

```bash
dotnet run --project LC1/LC1.csproj
```

Введите выражение в редактор и нажмите **Пуск** (Ctrl+R).

### Примеры

| Ввод | ПОЛИЗ | Результат |
|------|-------|-----------|
| `6 + 7 + 10 * 4` | `6 7 + 10 4 * +` | `53` |
| `(2 + 3) * 4` | `2 3 + 4 *` | `20` |
| `a + b * 2` | `a b 2 * +` | вычисление недоступно (есть идентификаторы) |

## Тетрады

Для `a + b * c` формируются, например:

| op | arg1 | arg2 | result |
|----|------|------|--------|
| *  | b    | c    | t1     |
| +  | a    | t1   | t2     |

# Лабораторная работа 7 Анализ и преобразование кода с использованием Clang и LLVM

##Цель работы:
Познакомиться с инструментарием Clang и LLVM, освоить получение абстрактного синтаксического дерева (AST) и промежуточного представления (LLVM IR) для кода на C/C++, научиться применять базовые оптимизации, строить графы потока управления (CFG), а также анализировать влияние оптимизаций на различные синтаксические конструкции языка.
-
Постановка задачи:
Установить Clang и LLVM;
Скомпилировать простой C-файл с использованием clang и получить его: абстрактное синтаксическое дерево (AST), промежуточное представление LLVM IR;
Использовать opt для применения базовой комплексной оптимизации (например, О2);
Построить граф потока управления (CFG) для оптимизированной программы;
Проанализировать результат, сделать выводы и ответить на контрольные вопросы.
Выполнить индивидуальное задание:
Тема: Вещественные константы

Задания:
1. Получите IR для -O0.
2. Получите IR для -O2. Произошло ли свертывание константы?
3. Примените -constprop, -globalopt, -ipsccp.
4. Сравните CFG.
5. Сделайте вывод о том, когда вещественная константа
вычисляется на этапе компиляции?

Основное задание:
Установка Clang и LLVM:

Индивидуальное задание:
Программа варианта
<img width="339" height="155" alt="image" src="https://github.com/user-attachments/assets/7372234c-6001-4cf8-bba7-4da54fe83168" />
1. Получение IR -O0:
```
santoro@santoro-VirtualBox:~/llvm-lab$ cd ~/llvm-lab
clang -S -emit-llvm -O0 -fno-discard-value-names area_pi.c -o out/area_pi-O0.ll
santoro@santoro-VirtualBox:~/llvm-lab$ cat out/area_pi-O0.ll
; ModuleID = 'area_pi.c'
source_filename = "area_pi.c"
target datalayout = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-i128:128-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

@PI = dso_local constant double 0x400921FB54442D18, align 8

; Function Attrs: noinline nounwind optnone uwtable
define dso_local i32 @main() #0 {
entry:
  %retval = alloca i32, align 4
  %r = alloca double, align 8
  %area = alloca double, align 8
  store i32 0, ptr %retval, align 4
  store double 2.000000e+00, ptr %r, align 8
  %0 = load double, ptr %r, align 8
  %mul = fmul double 0x400921FB54442D18, %0
  %1 = load double, ptr %r, align 8
  %mul1 = fmul double %mul, %1
  store double %mul1, ptr %area, align 8
  %2 = load double, ptr %area, align 8
  %conv = fptosi double %2 to i32
  ret i32 %conv
}

attributes #0 = { noinline nounwind optnone uwtable "frame-pointer"="all" "min-legal-vector-width"="0" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cmov,+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }

!llvm.module.flags = !{!0, !1, !2, !3, !4}
!llvm.ident = !{!5}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 8, !"PIC Level", i32 2}
!2 = !{i32 7, !"PIE Level", i32 2}
!3 = !{i32 7, !"uwtable", i32 2}
!4 = !{i32 7, !"frame-pointer", i32 2}
!5 = !{!"Ubuntu clang version 18.1.3 (1ubuntu1)"}
```
2. Получите IR для -O2. Произошло ли свертывание константы?

3. Примените -constprop, -globalopt, -ipsccp.

4. Сравните CFG.

5. Сделайте вывод о том, когда вещественная константа
вычисляется на этапе компиляции?
