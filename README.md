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

Цель работы:
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

# Основное задание:

**1.2 Установка Clang и LLVM:**
Команды:
```
sudo apt update
sudo apt install -y clang graphviz llvm-18-tools
```

**Программа основного задания**

<img width="281" height="331" alt="image" src="https://github.com/user-attachments/assets/3630b148-00af-40b4-a202-2cbf1adae2af" />

**1.3 Получение AST**
Команда:
```
clang -Xclang -ast-dump -fsyntax-only main.c
```

**Вывод:**
<img width="1207" height="685" alt="image" src="https://github.com/user-attachments/assets/b1307f04-9b10-47f7-8946-035b41a416f1" />

**1.4 Генерация LLVM IR**
Команда: 
```
clang -S -emit-llvm main.c -o main.ll
```

**1.5 Оптимизация IR**
Команды: 
```
clang -O0 -S -emit-llvm main.c -o main_O0.ll
clang -O2 -S -emit-llvm main.c -o main_O2.ll
Для сравнения двух файлов:
diff main_O0.ll main_O2.ll
```

**Результат**
```
santoro@santoro-VirtualBox:~/llvm-lab$ clang -S -emit-llvm main.c -o main.ll
santoro@santoro-VirtualBox:~/llvm-lab$ clang -O0 -S -emit-llvm main.c -o main_O0.ll
santoro@santoro-VirtualBox:~/llvm-lab$ clang -O2 -S -emit-llvm main.c -o main_O2.ll
santoro@santoro-VirtualBox:~/llvm-lab$ diff main_O0.ll main_O2.ll
8,15c8,11
< ; Function Attrs: noinline nounwind optnone uwtable
< define dso_local i32 @square(i32 noundef %0) #0 {
<   %2 = alloca i32, align 4
<   store i32 %0, ptr %2, align 4
<   %3 = load i32, ptr %2, align 4
<   %4 = load i32, ptr %2, align 4
<   %5 = mul nsw i32 %3, %4
<   ret i32 %5
---
> ; Function Attrs: mustprogress nofree norecurse nosync nounwind willreturn memory(none) uwtable
> define dso_local i32 @square(i32 noundef %0) local_unnamed_addr #0 {
>   %2 = mul nsw i32 %0, %0
>   ret i32 %2
18,29c14,16
< ; Function Attrs: noinline nounwind optnone uwtable
< define dso_local i32 @main() #0 {
<   %1 = alloca i32, align 4
<   %2 = alloca i32, align 4
<   %3 = alloca i32, align 4
<   store i32 0, ptr %1, align 4
<   store i32 5, ptr %2, align 4
<   %4 = load i32, ptr %2, align 4
<   %5 = call i32 @square(i32 noundef %4)
<   store i32 %5, ptr %3, align 4
<   %6 = load i32, ptr %3, align 4
<   %7 = call i32 (ptr, ...) ptr noundef @.str, i32 noundef %6
---
> ; Function Attrs: nofree nounwind uwtable
> define dso_local noundef i32 @main() local_unnamed_addr #1 {
>   %1 = tail call i32 (ptr, ...) ptr noundef nonnull dereferenceable(1 @.str, i32 noundef 25)
33c20,21
< declare i32 ptr noundef, ... #1
---
> ; Function Attrs: nofree nounwind
> declare noundef i32 ptr nocapture noundef readonly, ... local_unnamed_addr #2
35,36c23,25
< attributes #0 = { noinline nounwind optnone uwtable "frame-pointer"="all" "min-legal-vector-width"="0" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cmov,+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }
< attributes #1 = { "frame-pointer"="all" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cmov,+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }
---
> attributes #0 = { mustprogress nofree norecurse nosync nounwind willreturn memory(none) uwtable "min-legal-vector-width"="0" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cmov,+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }
> attributes #1 = { nofree nounwind uwtable "min-legal-vector-width"="0" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cmov,+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }
> attributes #2 = { nofree nounwind "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cmov,+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }
38,39c27,28
< !llvm.module.flags = !{!0, !1, !2, !3, !4}
< !llvm.ident = !{!5}
---
> !llvm.module.flags = !{!0, !1, !2, !3}
> !llvm.ident = !{!4}
45,46c34
< !4 = !{i32 7, !"frame-pointer", i32 2}
< !5 = !{!"Ubuntu clang version 18.1.3 (1ubuntu1)"}
---
> !4 = !{!"Ubuntu clang version 18.1.3 (1ubuntu1)"}
```

**1.6 Граф потока управления программы**
Команды которые мы используем:
```
clang -O2 -S -emit-llvm main.c -o out/main_O2.ll
clang -c -emit-llvm -O2 main.c -o out/main_O2.bc

cd ~/llvm-lab
rm -f .main.dot .square.dot

opt -passes=dot-cfg out/main_O2.bc -o out/tmp.bc

ls -la .*.dot

dot -Tpng .main.dot -o out/cfg_main.png
# если есть .square.dot:
dot -Tpng .square.dot -o out/cfg_square.png 2>/dev/null || true

rm -f .main.dot .square.dot out/tmp.bc
xdg-open out/cfg_main.png
```

# Вывод:
<img width="662" height="145" alt="image" src="https://github.com/user-attachments/assets/76bd3485-fd45-4f3d-b379-dc5a14a36650" />


# Индивидуальное задание:
Программа варианта

<img width="339" height="155" alt="image" src="https://github.com/user-attachments/assets/7372234c-6001-4cf8-bba7-4da54fe83168" />

**1. Получение IR -O0:**
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
**2. Получите IR для -O2. Произошло ли свертывание константы?**
```
santoro@santoro-VirtualBox:~/llvm-lab$ cd ~/llvm-lab
clang -S -emit-llvm -O2 -fno-discard-value-names area_pi.c -o out/area_pi-O2.ll
santoro@santoro-VirtualBox:~/llvm-lab$ cat out/area_pi-O2.ll
; ModuleID = 'area_pi.c'
source_filename = "area_pi.c"
target datalayout = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-i128:128-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

@PI = dso_local local_unnamed_addr constant double 0x400921FB54442D18, align 8

; Function Attrs: mustprogress nofree norecurse nosync nounwind willreturn memory(none) uwtable
define dso_local noundef i32 @main() local_unnamed_addr #0 {
entry:
  ret i32 12
}

attributes #0 = { mustprogress nofree norecurse nosync nounwind willreturn memory(none) uwtable "min-legal-vector-width"="0" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cmov,+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }

!llvm.module.flags = !{!0, !1, !2, !3}
!llvm.ident = !{!4}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 8, !"PIC Level", i32 2}
!2 = !{i32 7, !"PIE Level", i32 2}
!3 = !{i32 7, !"uwtable", i32 2}
!4 = !{!"Ubuntu clang version 18.1.3 (1ubuntu1)"}
```
**2.1 Произошло ли свертывание константы?**

При генерации IR с флагом -O2 LLVM выполнил свёртывание констант и распространение констант: выражение с вещественной константой PI и r = 2.0 вычислено на этапе компиляции, в теле main осталась только инструкция ret i32 12.

**3. Примените -constprop, -globalopt, -ipsccp.**
***3.1 -constprop***
```
opt -passes=sccp out/area_pi-O0.bc -S -o out/area_pi-constprop.ll
diff -u out/area_pi-O0.ll out/area_pi-constprop.ll
```

Применение opt -passes=sccp к полученному при -O0, не изменило алгоритм функции main: сохранились операции alloca, store, load, fmul с константой π и fptosi. В diff видны лишь переименование временных переменных и отсутствие метки entry, что связано с повторной генерацией текстового IR из bitcode, а не со свёртыванием вещественной константы. Вычисление PI * 2.0 * 2.0 на этапе компиляции при одном проходе sccp не выполняется.

***3.2 -globalopt***
```
opt -passes=globalopt out/area_pi-O0.bc -S -o out/area_pi-globalopt.ll
diff -u out/area_pi-O0.ll out/area_pi-globalopt.ll
```

Проход globalopt изменил объявление @PI, но логику main не упростил: умножения и работа с памятью остались. Константное выражение при одном globalopt не вычисляется.

***3.3 -ipsccp***
```
opt -passes=ipsccp out/area_pi-O0.bc -S -o out/area_pi-ipsccp.ll
diff -u out/area_pi-O0.ll out/area_pi-ipsccp.ll
```
opt -passes=ipsccp не упростил IR: в main остались те же операции с памятью и fmul; из‑за одной функции и хранения значений в alloca межпроцедурная пропагация констант почти не сработала.

**4. Сравните CFG.**
Команды для получения PNG:
```
cd ~/llvm-lab

# O0
opt -passes=dot-cfg out/area_pi-O0.bc -o out/tmp-O0.bc
cp .main.dot out/cfg-O0.dot
dot -Tpng .main.dot -o out/cfg-O0.png
rm -f .main.dot out/tmp-O0.bc
```
<img width="497" height="301" alt="image" src="https://github.com/user-attachments/assets/1baa4be6-b032-49c8-9ebc-9bc2b027d42a" />

```
# O2
opt -passes=dot-cfg out/area_pi-O2.bc -o out/tmp-O2.bc
cp .main.dot out/cfg-O2.dot
dot -Tpng .main.dot -o out/cfg-O2.png
rm -f .main.dot out/tmp-O2.bc
```
<img width="165" height="76" alt="image" src="https://github.com/user-attachments/assets/25845023-a755-4ce4-89be-512873aafcbd" />

# Выводы по совершенной работе
При -O0 CFG функции main — один базовый блок со всеми операциями в памяти и с плавающей точкой.
При -O2 — тоже один блок, но он сводится к ret i32 12.
Число блоков и переходов не меняется; упрощается содержимое блока за счёт свёртывания констант.

Вещественная константа PI и выражение PI * r * r не вычисляются на этапе компиляции при генерации IR с -O0: в промежуточном коде остаются загрузки константы и операции fmul с плавающей точкой, что подтверждается CFG с полным набором инструкций в одном базовом блоке.

На этапе компиляции выражение вычисляется при использовании clang -O2: в IR функции main остаётся только ret i32 12, а CFG сводится к одному блоку с этой инструкцией. Это означает, что LLVM заранее посчитал площадь и привёл результат к целому.

Отдельные проходы sccp (constprop), globalopt и ipsccp для IR уровня -O0 не дают такого эффекта: константное выражение с вещественной PI сворачивается только в составе комплексной оптимизации -O2, когда работают совместно преобразование памяти, распространение и свёртывание констант.

# Ответы на контрольные вопросы:

1. Что такое Clang, и какова его роль в процессе компиляции программ?
```
Clang — это фронтенд компилятора для языков C, C++ и Objective-C. Он разбирает исходный текст, строит абстрактное синтаксическое дерево и формирует промежуточное представление LLVM IR.
```
2. Что представляет собой LLVM и как он используется в
современных компиляторах?
```
LLVM — набор библиотек и утилит для компиляторов. На основе IR он выполняет оптимизации и генерирует машинный код под разные платформы.
```
3. Чем отличается абстрактное синтаксическое дерево (AST) от
промежуточного представления LLVM IR?
```
AST описывает структуру и синтаксис программы «как в языке». LLVM IR — уже низкоуровневый набор инструкций, удобный для оптимизаций и генерации кода.
```
4. Для чего необходимо промежуточное представление (IR) в процессе компиляции?
```
IR даёт единый промежуточный формат: с ним проще анализировать и оптимизировать программу, не привязываясь к конкретному языку или процессору.
```
5. Что делает инструкция alloc в LLVM IR, и зачем она используется в функциях?
```
Инструкция alloca резервирует место на стеке под локальную переменную. В неоптимизированном IR через неё часто представляют a, b и подобные переменные.
```
6. Зачем нужна оптимизация кода в компиляторе, и какие основные цели она преследует?
```
Оптимизации ускоряют выполнение, уменьшают размер программы и убирают избыточные операции, которые можно вычислить заранее или упростить.
```
7. Что такое SSA-форма и почему она важна при оптимизации программ?
```
SSA (Static Single Assignment) — форма IR, в которой каждому значению соответствует одно присваивание. Это облегчает отслеживание данных и применение оптимизаций.
```
8. Что такое граф потока управления (CFG) и как он помогает анализировать поведение программы?
```
CFG (граф потока управления) — схема базовых блоков и переходов между ними. По CFG видно, в каком порядке и при каких условиях выполняются части функции.
```
9. Как устроено представление арифметических операций в LLVM IR (например, умножение, сложение)?
```
Арифметика задаётся отдельными инструкциями (add, sub, mul, fmul, icmp и т.д.) с указанием типа и операндов-регистров.
```
10. Почему функции в LLVM IR обычно представляют собой отдельные единицы анализа и оптимизации?
```
Каждую функцию можно разбирать, оптимизировать, встраивать или удалять отдельно, не затрагивая остальной код модуля.
```
11. Что происходит с функцией в LLVM IR, если она вызывается один раз и очень короткая?
```
Компилятор часто встраивает её в вызывающий код (inlining). После оптимизаций отдельная функция может исчезнуть из IR, как square при -O2 в main.c.
```
12. Какие преимущества даёт использование IR и CFG для автоматических оптимизаций по сравнению с анализом исходного текста на C?
```
IR и CFG имеют строгую, однозначную структуру без синтаксического «шума» языка C. Это упрощает автоматический анализ и применение оптимизаций.
```
# Дополнительное задание
Вариант 54: Объявление вещественной константы на языке Kotlin
Верная конструкция: const val x: Double = (((5) + ((3 * 0))) - ((4 - 2))) + 0 * 1 - 0 + (0 * 999) + (0);

**Оптимизация №1: Свертывание констант**
Подразумевает вычисление арифметики и упрощения над уже известными числами на этапе компиляции (например, 3 * 0 → 0, 5 + 0 → 5, (4 - 2) → 2) и замену цепочки операций в IR одним литералом.

**Оптимизация №2 Удаление мертвого кода**
Подразумевает удаление неиспользуемых LOAD_CONST, ADD, MUL и временных t1, t2, … после свёртки и приведение объявления к каноническому виду: DECLARE_CONST id, Double, <итоговое значение>.

**Тестовый пример**
<img width="867" height="538" alt="image" src="https://github.com/user-attachments/assets/9319dc1b-401a-4c18-9b4e-aa5e2a4f735b" />


Первая оптимизация:

<img width="862" height="641" alt="image" src="https://github.com/user-attachments/assets/5681b3f7-b0fd-4362-85ae-96cae74dbda0" />
<img width="861" height="734" alt="image" src="https://github.com/user-attachments/assets/d3b81bbd-ee16-4abb-9052-1fc6226916d7" />


Вторая оптимизация: 

<img width="1081" height="999" alt="image" src="https://github.com/user-attachments/assets/4ef5a0d2-1c69-4bed-b30e-d03a25372b76" />
