# FixFlow.Api

[![CI](https://github.com/mpalus-git/FixFlow.Api/actions/workflows/ci.yml/badge.svg)](https://github.com/mpalus-git/FixFlow.Api/actions/workflows/ci.yml)
[![Licencja: MIT](https://img.shields.io/badge/licencja-MIT-blue.svg)](LICENSE)

## Opis systemu

FixFlow to backend systemu obsługi zleceń serwisowych w terenie dla firmy naprawiającej klimatyzację i urządzenia biurowe. Dyspozytor prowadzi kartotekę klientów, urządzeń i części, tworzy zlecenia i przypisuje je technikom. Technik realizuje zlecenie w terenie: rozpoczyna pracę, dodaje wpisy serwisowe z czasem pracy, zdjęciami i zużytymi częściami, a na końcu zamyka zlecenie i pobiera protokół serwisowy w PDF. Repozytorium zawiera wyłącznie API; klienci (panel webowy React i aplikacja mobilna MAUI) korzystają z kontraktu opisanego w [openapi/v1.json](openapi/v1.json).

Działająca instancja: [fixflow-api-us2p.onrender.com/scalar](https://fixflow-api-us2p.onrender.com/scalar). Usługa działa na darmowym planie Render, więc pierwsze wejście po okresie bezczynności może trwać do minuty. Konta demonstracyjne:

| Rola | E-mail | Hasło |
|---|---|---|
| Dispatcher | `dispatcher@fixflow.local` | `cyTfum-duwcyr-kyjky3` |
| Technician | `technician@fixflow.local` | `fembag-wupker-Retfi7` |

Token otrzymany z `POST /api/v1/auth/login` wkleja się w Scalar jako `Bearer`.

## Architektura

```mermaid
flowchart LR
    web["Panel dyspozytora (React)"] --> endpoints
    mobile["Aplikacja technika (MAUI)"] --> endpoints

    subgraph api["FixFlow.Api"]
        endpoints["Minimal API /api/v1<br/>walidacja, autoryzacja"] --> handlers["Handlery funkcji<br/>(Vertical Slice)"]
        jobs["Joby Quartz<br/>opóźnienia, podsumowanie"] --> handlers
        handlers --> domain["Domena<br/>encje i reguły"]
        handlers --> cache["HybridCache<br/>L1 w pamięci"]
    end

    handlers --> postgres[("PostgreSQL 17")]
    cache -. "L2, opcjonalnie" .-> redis[("Redis 7")]
    handlers --> smtp["SMTP<br/>(lokalnie Mailpit)"]
```

Każda funkcja ma własny folder w `src/FixFlow.Api/Features/<Obszar>/<Funkcja>/` z endpointem, handlerem, walidatorem oraz rekordami żądania i odpowiedzi. Reguły biznesowe są w encjach w `Domain/`, a elementy wspólne (persystencja, autoryzacja, mapowanie błędów, cache, e-mail, joby) w `Common/`.

## Uruchomienie lokalne

Wymagany jest Docker. Całe środowisko (API, PostgreSQL, Redis, Mailpit) startuje jedną komendą:

```bash
cp .env.example .env
docker compose up --build
```

Przed startem trzeba uzupełnić w `.env`:

| Zmienna | Wartość |
|---|---|
| `POSTGRES_PASSWORD` | dowolne hasło lokalnej bazy |
| `JWT_SIGNING_KEY` | losowy klucz, co najmniej 32 bajty, np. wynik `openssl rand -base64 48` |
| `DEMO_ADMIN_PASSWORD`, `DEMO_DISPATCHER_PASSWORD`, `DEMO_TECHNICIAN_PASSWORD` | hasła kont demo: co najmniej 8 znaków, mała i wielka litera, cyfra, znak specjalny |

Po starcie dostępne są:

| Adres | Co |
|---|---|
| http://localhost:8080/scalar | dokumentacja i klient API |
| http://localhost:8080/openapi/v1.json | dokument OpenAPI |
| http://localhost:8080/health | liveness (bez bazy) |
| http://localhost:8080/health/ready | readiness (z bazą) |
| http://localhost:8025 | Mailpit, podgląd wysłanych e-maili |

Migracje bazy wykonują się przy starcie aplikacji. Konta demo (`admin@fixflow.local`, `dispatcher@fixflow.local`, `technician@fixflow.local`) zakładane są z hasłami z `.env`.

Testy wymagają .NET SDK 10 i działającego Dockera (testy integracyjne uruchamiają PostgreSQL i Redis przez Testcontainers):

```bash
dotnet test
```

## Konfiguracja

Ustawienia można podać w `appsettings.json` lub jako zmienne środowiskowe (separator `__`, np. `Jwt__SigningKey`).

| Klucz | Opis | Domyślnie |
|---|---|---|
| `ConnectionStrings__Database` | connection string PostgreSQL (Npgsql) | brak, wymagany |
| `ConnectionStrings__Redis` | connection string Redis; pusty oznacza cache tylko w pamięci | pusty |
| `Jwt__SigningKey` | klucz podpisu tokenów, co najmniej 32 bajty | brak, wymagany |
| `Jwt__AccessTokenLifetime` / `Jwt__RefreshTokenLifetime` | czas życia tokenów | `00:15:00` / `7.00:00:00` |
| `Cors__AllowedOrigins__0` | dozwolone originy klientów przeglądarkowych (kolejne pod `__1`, `__2`) | brak |
| `RateLimiting__Auth__PermitLimit` / `RateLimiting__Auth__Window` | limit logowania i odświeżania tokena na adres IP | `10` / `00:01:00` |
| `Seed__DemoUsers__Enabled` | zakładanie kont demo przy starcie | `false` |
| `Seed__DemoUsers__AdminPassword`, `...DispatcherPassword`, `...TechnicianPassword` | hasła kont demo, wymagane przy włączonym seedzie | brak |
| `Email__Enabled` | wysyłka e-mail; wyłączona oznacza tylko wpis w logu | `false` |
| `Email__Host`, `Email__Port`, `Email__Security` | serwer SMTP; `Security` przyjmuje `None`, `Auto`, `SslOnConnect`, `StartTls` | brak, `587`, `Auto` |
| `Email__Username`, `Email__Password` | dane logowania SMTP, opcjonalne | brak |
| `Email__FromAddress`, `Email__FromName` | nadawca wiadomości | `noreply@fixflow.local`, `FixFlow` |
| `Jobs__Enabled` | uruchamianie jobów Quartz | `true` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | odczyt `X-Forwarded-For` i `X-Forwarded-Proto` za reverse proxy | `false` |
| `ForwardedHeaders__ForwardLimit` | liczba zaufanych proxy w łańcuchu `X-Forwarded-For` | `1` |

## Reguły biznesowe

Zlecenie przechodzi przez statusy `New -> Assigned -> InProgress -> Completed -> Invoiced`. Jedynym dozwolonym cofnięciem jest `Assigned -> New` (odpięcie technika), każde inne przejście kończy się `409 WorkOrder.InvalidStatusTransition`.

| Reguła | Gdzie jest wymuszona | Błąd |
|---|---|---|
| 1. Technik ma najwyżej jedno zlecenie w statusie `InProgress` | metoda encji `WorkOrder.Start` oraz częściowy unikalny indeks `work_orders(technician_id) WHERE status = 'InProgress'` na wypadek równoległych żądań | `409 WorkOrder.TechnicianAlreadyHasWorkInProgress` |
| 2. Zakończenie zlecenia wymaga co najmniej jednego wpisu serwisowego | `WorkOrder.Complete`; wyścig między dodaniem wpisu a zakończeniem rozstrzyga token współbieżności `xmin` | `409 WorkOrder.NoServiceEntries` |
| 3. Zużycie części zmniejsza stan magazynowy, stan nie może spaść poniżej zera | encja `Part` oraz `CHECK (stock_quantity >= 0)` i token `xmin` na tabeli `parts`; pomyłkę koryguje wpis zwracający części na magazyn | `409 Part.InsufficientStock` |
| 4. Zlecenie po terminie, niezakończone, dostaje flagę `IsOverdue` | job Quartz uruchamiany co godzinę; flaga jest też przeliczana od razu po zmianie terminu i zerowana przy zakończeniu | - |
| 5. Technika przypisuje tylko Dispatcher lub Admin; technik widzi i zmienia wyłącznie swoje zlecenia | polityki autoryzacji endpointów i filtr widoczności w zapytaniach; cudze zlecenie jest dla technika nieistniejące | `403`, `404 WorkOrder.NotFound` |
| 6. Numer seryjny urządzenia i numer katalogowy części są unikalne | normalizacja (przycięcie, wielkie litery) i unikalne indeksy; naruszenie `23505` z PostgreSQL mapowane na konflikt | `409 Device.DuplicateSerialNumber`, `409 Part.DuplicateCatalogNumber` |

Każda reguła ma testy: jednostkowe dla metod encji i integracyjne dla endpointów na prawdziwej bazie PostgreSQL.

Błędy API mają format ProblemDetails (RFC 9457) z dodatkowym polem `errorCode`, na którym klient może opierać logikę zamiast na treści komunikatu:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Technician already has another work order in progress.",
  "errorCode": "WorkOrder.TechnicianAlreadyHasWorkInProgress"
}
```

## Decyzje techniczne

**Vertical Slice zamiast warstw.** Zmiana funkcji (np. przypisanie technika) dotyka jednego folderu z endpointem, handlerem, walidatorem i kontraktem, zamiast pięciu projektów i wspólnych serwisów. Handlery używają `DbContext` bezpośrednio, bez repozytoriów, a reguły biznesowe siedzą w encjach domeny. Gdy reguła potrzebuje wiedzy spoza encji, handler przekazuje ją jako argument, np. `workOrder.Start(technicianHasWorkInProgress)`, dzięki czemu reguła jest testowalna jednostkowo.

**ErrorOr zamiast wyjątków dla błędów biznesowych.** Odrzucenie przejścia statusu czy brak części na magazynie to oczekiwany wynik operacji, a nie sytuacja wyjątkowa. Metody domeny i handlery zwracają `ErrorOr<T>`, a mapowanie na ProblemDetails i kody HTTP odbywa się w jednym miejscu (`Common/Errors`). Wyjątki zostają dla błędów rzeczywiście nieoczekiwanych.

**Cache musi działać bez Redis.** Listy klientów i urządzeń są cache'owane przez `HybridCache`: L1 w pamięci procesu, L2 w Redis. Redis przyspiesza odczyty, ale nie jest źródłem prawdy, więc jego awaria nie może zatrzymać API. Pusty connection string oznacza brak L2. Skonfigurowany, ale niedostępny Redis ma krótkie timeouty i opakowanie, które traktuje błąd jak brak wpisu, zamiast zwracać 500. Unieważnianie odbywa się przez tagi po każdym zapisie. Test integracyjny sprawdza działanie API przy wyłączonym Redis.

**Reguły narażone na współbieżność zabezpieczone także w bazie.** Sprawdzenie w kodzie nie wystarcza przy dwóch równoległych żądaniach, dlatego reguły 1, 3 i 6 mają odpowiednik w postaci indeksu, ograniczenia `CHECK` lub tokenu współbieżności, a naruszenie jest tłumaczone na `409`.

**Kontrakt API pilnowany w CI.** Dokument `openapi/v1.json` jest generowany przy buildzie i commitowany. CI odrzuca zmianę, jeśli wygenerowany dokument różni się od tego w repozytorium, więc każda zmiana kontraktu jest widoczna w review. API jest wersjonowane w ścieżce (`/api/v1`).

**Czas przez `TimeProvider`.** Kod nie odwołuje się do `DateTime.UtcNow`; testy reguły opóźnień i podsumowania dziennego sterują czasem przez `FakeTimeProvider`. Daty są przechowywane jako `timestamptz` w UTC, a strefa `Europe/Warsaw` jest używana tylko do prezentacji (e-mail, protokół PDF).
