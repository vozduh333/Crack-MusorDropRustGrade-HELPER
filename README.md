# 🔬 MusorDropHelper / RustGradeHelper — Research
🇬🇧🇬🇧english
This repository is for documenting research on MusorDropHelper and RustGradeHelper. It's not connected to the original developer. The goal is to document the software, how it works, and how it's distributed.

Some of this was figured out through reverse engineering. You'll find recovered source code for MusorDropHelper, compiled files, and the RustGradeHelper executable here.

The information is for research, learning, archiving, and making things work together.

You should look at the technical proof here instead of just trusting claims about the software or the person who made it.


## 🗑️ What is MusorDropHelper?

MusorDropHelper is a tool that works with the Rustgrade live-drop feed. It watches recent item drops and tries to guess if the current feed is in a good or bad phase.

It's important to know that this program doesn't predict the exact next item. It doesn't say The next drop will be an expensive knife.

Instead, it looks at past drops and gives a general score showing the current heat of the feed.

So, it's more like a historical pattern indicator than a predictor of the next drop.


## ⚙️ How the prediction system actually works

The process is roughly:

**Live drop feed → Recent drop history → Price/rarity stats → Built-in rules → Score → EMA smoothing → COLD / WARM / HOT / FIRE**

The program gets data about drops that have already happened.

It then examines recent history and calculates simple stats like average price, recent average price, how many drops were expensive or cheap, how many high-value items appeared, streaks of expensive or cheap drops, drop frequency, and item rarity.

The analyzer then combines these numbers into a score that was designed by hand.


## 🔥 The heat system

The analyzer categorizes drops using basic thresholds.

For example, cheap items are low-value drops, while expensive items and certain rarities increase the heat of the feed.

Basically, it works like this:

- 🟥 cheap item → negative sign
- 🟩 expensive item → positive sign
- 💎 rare item → positive sign
- 📉 many cheap drops → negative sign
- 📈 many expensive drops → positive sign

The actual code uses set rules and limits, not a model that knows the future.

This means the program asks, Does the recent history look good based on my rules?

It doesn't answer, What will the server generate next?


## 📊 Recent-drop window

The analyzer looks at a recent window of drops, not all the history.

It uses about the last 50 drops for its calculations.

The program can then compare things like the overall recent average versus a short-term average, and cheap drops versus expensive drops.

It can also track consecutive events.

For instance, a sequence of low-priced items might be seen as a cold or dry period.

If several expensive items appear later, the score can go up.


## ⚠️ The main weakness of the prediction logic

The biggest issue is that the system might see a series of bad drops as a sign that a good period is coming.

For example:

**cheap, cheap, cheap, cheap, cheap, cheap → dry period → higher score**

But past results don't guarantee the next one will be better.

If the server's results are random and independent, then 10 cheap drops in a row doesn't mean the 11th drop has to be expensive.

This is a basic limit of the system.

The program can find patterns in past data, but finding a pattern isn't the same as proving it can predict the future.


## 🎯 The Score is NOT a probability

This is another important point.

If the program shows a score of 82, it doesn't mean there's an 82% chance of a good drop.

The score is just what the internal system outputs.

Conceptually, it's historical data plus built-in rules leading to an internal score.

It's not Score = actual probability of the next drop.

Unless the developers provide proof that this relationship exists, treating the score as a real probability would be misleading.


## 📈 EMA smoothing

The score is also smoothed using an exponential moving average (EMA).

This is mainly to stop the displayed indicator from changing too wildly.

Instead of going from 40 to 90 to 30 to 100 to 20, the displayed value changes more gradually.

This makes the indicator look more stable.

However, EMA doesn't give the program any new information about future drops; it just smooths the score that was already figured out.


## 🌡️ COLD / WARM / HOT / FIRE

The final score can be turned into easy-to-understand states like COLD, WARM, HOT, or FIRE.

These labels make the result easier to see.

However, they shouldn't be taken as guaranteed predictions.

For example, FIRE doesn't mean A valuable item is definitely going to drop.

It means the recent historical data meets enough of the program's rules to produce a high score.


## 🌐 Where does the data come from?

The system uses live-drop data, not some public value for the next drop.

The related Rustgrade web app has endpoints like /api/live-drops and /api/live-drops/best-hour.

The interface uses this data to show a live-drop feed and find a best drop for the time period observed.

The key thing is that this is observed historical or live-feed information, not a revealed future outcome.


## 🚫 What the system does NOT demonstrate

The logic analyzed doesn't show a process like:

**retrieve server RNG state → retrieve future seed → calculate next server roll → predict exact item**

Instead, the general structure is:

**observe previous drops → calculate statistics → apply heuristic rules → calculate score → display heat**

That difference is very important.


## 🧠 So what is MusorDropHelper actually?

Simply put, MusorDropHelper doesn't really answer what will drop next?

It answers something closer to, Based on recent drops, does the current feed look good or bad according to the rules programmed into the tool?

That can still be useful as a visualization or a statistical experiment.

But calling it a real prediction engine would require proof that its signals actually relate to future results.

Without that proof, it's fundamentally a heuristic system.


## 📂 Why publish the source?

The source is being published so others can look at the code themselves.

Instead of saying Trust the developer, the predictor works, the goal of this repository is:

**Here's how it's made. Look at it yourself.**

Anyone can examine the code, understand how the score is calculated, compare the code to the distributed programs, and decide for themselves if the system can be called a predictor.


## 🗑️ MusorDropHelper

The recovered project has several parts:

- `MusorDropHelper/`
- `MusorDropHelper.Core/`
- `MusorDropHelper.Protection/`
- `MusorDropHelper.Ui/`

The project includes parts for feed/network communication, drop analysis, scoring, licensing, activation, crypto/shared functions, the user interface, and anti-debugging measures.

A compiled version is also in the Releases section.

The published release might use the simpler filename `MusorDropHelper.exe` instead of the original `MusorDropHelper-nolic.exe`.

The filename doesn't change what's inside the executable.


## 🦀 RustGradeHelper

RustGradeHelper is provided as a compiled executable.

Unlike MusorDropHelper, this repository doesn't claim to have the original RustGradeHelper source code.

The executable is included as a separate research item for comparison and independent analysis.


## 👤 About the original developer

Another reason for creating this repository is how the original software was distributed and how criticism about the project was handled.

From our view, there are good reasons to be suspicious of how the developer presented the software.

Concerns include closed distribution, licensing and access controls, lack of transparency about how it works, strong efforts to prevent the software from being examined, negative responses to criticism, and deletion of messages and blocking of users in related communities after disagreements.

These things don't automatically prove the software is harmful or fake.

However, they do give a reason to examine the software independently rather than just believing the developer's claims.

This repository therefore focuses on making the technical material available for people to look at themselves.


## ⚖️ Criticism of the software vs. accusations

This repository is not meant to encourage harassment of the original developer.

The criticism here is mainly technical:

- How does the prediction algorithm actually work?
- What data does it use?
- What does the score mean?
- Is the score actually predictive?
- Does the code match how the software is described?
- Can the claimed features be confirmed independently?

Readers should tell the difference between documented technical facts, observations, and opinions.


## 📦 Repository contents

```text
Crack-MusorDropRustGrade-HELPER/
│
├── README.md
│
├── RustGradeHelper.exe
│
├── MusorDropHelper/
│   ├── source/
│   │   ├── MusorDropHelper/
│   │   ├── MusorDropHelper.Core/
│   │   ├── MusorDropHelper.Protection/
│   │   └── MusorDropHelper.Ui/
│   │
│   ├── MusorDropHelper.dll
│   ├── MusorDropHelper.deps.json
│   └── MusorDropHelper.runtimeconfig.json
│
└── Releases/
    └── MusorDropHelper.exe












     🇷🇺 MusorDropHelper — Русская версия

## 🗑️ Что такое MusorDropHelper?

MusorDropHelper — это программа, которая следит за дропами в Rustgrade и пытается понять, хорошая или плохая текущая серия дропов.

Но важно понимать:

> ⚠️ программа не говорит, какой предмет выпадет следующим.

Она берет уже произошедшие дропы, анализирует их и по заранее заданным правилам считает условный Score.

### 🔄 Вот как это примерно работает:

```text
Live-дропы
    ↓
История последних дропов
    ↓
Статистика
    ↓
Набор правил, заданных вручную
    ↓
Score
    ↓
EMA-сглаживание
    ↓
COLD / WARM / HOT / FIRE
🔮 Как она пытается «предсказывать»

Программа смотрит на такие вещи, как:

💰 цена предметов;
💎 их редкость;
📊 средняя стоимость;
📈 краткосрочная средняя стоимость;
🔥 сколько было дорогих предметов;
🥶 сколько было дешевых предметов;
🔥 серии дорогих дропов;
🥶 серии дешевых дропов;
⏱️ как часто появлялись дропы.

Потом применяются заранее заданные коэффициенты.

Например:

много дешевых предметов
        ↓
плохой сигнал

много дорогих предметов
        ↓
хороший сигнал

редкий/дорогой предмет
        ↓
хороший сигнал

длинная серия дешевых дропов
        ↓
«сушь»

после нее дорогой дроп
        ↓
«разогрев»

Потом все это превращается в один Score.

🧠 Почему это довольно простой предиктор

Главная проблема в том, что программа анализирует прошлые результаты, а не получает от сервера информацию о будущих.

Например:

$0.50
$0.80
$1.20
$0.70
$1.10
$0.60

программа может посчитать это плохой серией.

После этого Score может вырасти, ожидая, что ситуация «разогревается».

Но из того, что предыдущие дропы были дешевыми, не следует, что следующий обязательно будет дорогим.

Если результаты действительно независимы, то:

10 плохих дропов
        ↓
не делают
        ↓
11-й дроп
        ↓
автоматически хорошим

Поэтому прошлые паттерны нельзя автоматически считать настоящим прогнозом.

🎯 Score ≠ вероятность

Если программа показывает:

Score: 82

это не значит:

82% шанс хорошего дропа

Это просто внутренний показатель, который программа получила из своих правил.

То есть:

прошлые дропы
     ↓
статистика
     ↓
ручные коэффициенты
     ↓
Score

а не:

Score 82 = реальная вероятность 82%

Чтобы такой показатель можно было назвать реальной вероятностью, нужна статистическая проверка на большом количестве независимых данных.

📈 EMA

После расчета Score программа использует EMA — экспоненциальное сглаживание.

Это делает индикатор визуально более стабильным.

Вместо:

40 → 90 → 30 → 100 → 20

значение меняется плавно.

Но EMA не предсказывает будущее.

Она просто сглаживает уже полученный показатель.

🌡️ COLD / WARM / HOT / FIRE

После расчета Score программа показывает состояние:

🥶 COLD
🌤️ WARM
🔥 HOT
🚀 FIRE

Это удобный способ показать результат визуально.

Но:

🚨 FIRE не означает:
«Следующим точно выпадет дорогой предмет».

Это означает только:

«По правилам программы, текущая история дропов выглядит очень горячей».

📊 Что программа реально знает

Она примерно знает следующее:

Что уже выпало?
Сколько это стоило?
Какие были редкости?
Было ли много дорогих предметов?
Была ли серия дешевых предметов?
Как изменилась средняя цена?

Но это совсем не то же самое, что знать:

❓ Что выпадет следующим?

Поэтому самое точное описание:

🧩 MusorDropHelper правильнее всего описывать как:

эвристический индикатор состояния live-дропов

а не как:

реальный предиктор следующего дропа.

Именно поэтому исходники интересны: любой может посмотреть код и сам понять, насколько сложна система на самом деле.

🧩 Итог

По сути, программа делает так:

50 последних дропов
       ↓
цены / редкости / серии
       ↓
несколько if/else
       ↓
ручные коэффициенты
       ↓
Score
       ↓
EMA
       ↓
COLD / WARM / HOT / FIRE

То есть за довольно эффектным интерфейсом «предсказателя» скрывается относительно простой набор эвристик.

Это не обязательно означает, что программа бесполезна — такой индикатор может показывать статистические изменения в live-feed.

Но чтобы доказать, что он действительно предсказывает будущие дропы, нужна отдельная статистическая проверка на исторических данных.

Без такой проверки высокий Score остается просто оценкой программы, а не доказанной вероятностью получить дорогой предмет.
