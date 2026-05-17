# Лабораторная работа 6 : Создание внутренней формы представления программы

## Цель

Изучить методы построения внутреннего представления программы (ВПП) на основе контекстно-свободной грамматики, реализовать синтаксический анализатор методом рекурсивного спуска и преобразовать арифметические выражения в тетрады и ПОЛИЗ.

## Автоор
Гидульянов Кирилл Сергеевич АВТ-313

## Постановка задачи
1. Реализовать поиск лексических и синтаксических ошибок для заданной КС-грамматики методом рекурсивного спуска.

2. Представить внутреннюю форму программы в виде тетрад (op, arg1, arg2, result) для арифметических выражений (только для корректных строк).

3. Преобразовать выражение в ПОЛИЗ (польскую инверсную запись) и вычислить его значение (только арифметическое выражение из целых чисел).

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

## Примеры ввернух строк

```
6 + 7 + 10 * 4 → 53
(2 + 3) * 4 → 20
100 / 5 - 3 → 17
```
# Диаграмма лексера

<img width="608" height="846" alt="image" src="https://github.com/user-attachments/assets/2a27b488-0034-459b-899f-2cd21578bfdb" />


# Схема рекурсивного спуска

<img width="486" height="464" alt="image" src="https://github.com/user-attachments/assets/a65f92e9-9c5e-462f-9429-2d1465ac66a1" />


# Пример работы лексера
**Пример №1**

<img width="1919" height="638" alt="image" src="https://github.com/user-attachments/assets/6679ad2a-9bc8-4117-a284-24c26682df2f" />


**Пример №2**

<img width="1920" height="661" alt="image" src="https://github.com/user-attachments/assets/b47c3693-cc57-442f-a691-6e264a94c887" />

**Пример №3**

<img width="1920" height="635" alt="image" src="https://github.com/user-attachments/assets/c01c90be-9d0c-44bf-8dc5-5b8cc2278189" />

## Пример работы парсера
**Пример №1**

<img width="1920" height="556" alt="image" src="https://github.com/user-attachments/assets/d374bf46-6413-4736-950d-ef41edd72de6" />


**Пример №2**

<img width="1920" height="543" alt="image" src="https://github.com/user-attachments/assets/d7a035b1-fb01-422e-8e92-2610af9fb591" />


**Пример №3**

<img width="1910" height="559" alt="image" src="https://github.com/user-attachments/assets/8bc3c36b-6c36-498b-8cef-1bfb9c234604" />

## Примеры разбиения на тетрады, формирования ПОЛИЗ и вычисления выражения:

**Пример №1**

<img width="1920" height="552" alt="image" src="https://github.com/user-attachments/assets/760a057c-8de1-4380-945a-3902405523a7" />

<img width="1920" height="569" alt="image" src="https://github.com/user-attachments/assets/1c2e8942-5d45-4937-8e27-24b295af2733" />



**Пример №2**

<img width="1920" height="531" alt="image" src="https://github.com/user-attachments/assets/f29752f8-0113-4121-9939-964948f926c6" />

<img width="1920" height="571" alt="image" src="https://github.com/user-attachments/assets/320fe4ee-e031-4617-975e-173221f277cb" />

**Пример №3**

<img width="1920" height="554" alt="image" src="https://github.com/user-attachments/assets/11153967-6dd0-4415-bf27-0d9162399d3d" />

<img width="1920" height="594" alt="image" src="https://github.com/user-attachments/assets/4742af4e-ace4-4c7b-b03d-1663998abd12" />

