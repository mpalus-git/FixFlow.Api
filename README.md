# FixFlow.Api

[![CI](https://github.com/mpalus-git/FixFlow.Api/actions/workflows/ci.yml/badge.svg)](https://github.com/mpalus-git/FixFlow.Api/actions/workflows/ci.yml)

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
        jobs["Joby Quartz<br/>opóźnienia, podsumowanie,<br/>sprzątanie tokenów"] --> handlers
        handlers --> domain["Domena<br/>encje i reguły"]
        handlers --> cache["HybridCache<br/>L1 w pamięci"]
    end

    handlers --> postgres[("PostgreSQL 18")]
    cache -. "L2, opcjonalnie" .-> redis[("Redis 8.4")]
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
| `OpenTelemetry__ConsoleExporterEnabled` | ślady i metryki OpenTelemetry wypisywane na konsolę | `false` |

Po włączeniu OpenTelemetry aplikacja zbiera ślady żądań HTTP (z trasą endpointu), zapytań do PostgreSQL i wywołań wychodzących oraz metryki ASP.NET Core, Kestrel, klienta HTTP i środowiska uruchomieniowego .NET. Eksporter konsolowy pisze zwykły tekst, który w kontenerze mieszałby się z logami JSON, dlatego jest domyślnie wyłączony i służy do diagnostyki. Logi Serilog zawierają identyfikatory śladu (`@tr`, `@sp`), więc wpis w logu można powiązać ze śladem.

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

Błędy walidacji (400) mają słownik `errors`, którego klucze są ścieżkami pól w camelCase, tak jak w JSON żądania (np. `address.postalCode`, `parts[0].partId`).

## Decyzje techniczne

**Vertical Slice zamiast warstw.** Zmiana funkcji (np. przypisanie technika) dotyka jednego folderu z endpointem, handlerem, walidatorem i kontraktem, zamiast pięciu projektów i wspólnych serwisów. Handlery używają `DbContext` bezpośrednio, bez repozytoriów, a reguły biznesowe siedzą w encjach domeny. Gdy reguła potrzebuje wiedzy spoza encji, handler przekazuje ją jako argument, np. `workOrder.Start(technicianHasWorkInProgress)`, dzięki czemu reguła jest testowalna jednostkowo.

**ErrorOr zamiast wyjątków dla błędów biznesowych.** Odrzucenie przejścia statusu czy brak części na magazynie to oczekiwany wynik operacji, a nie sytuacja wyjątkowa. Metody domeny i handlery zwracają `ErrorOr<T>`, a mapowanie na ProblemDetails i kody HTTP odbywa się w jednym miejscu (`Common/Errors`). Wyjątki zostają dla błędów rzeczywiście nieoczekiwanych.

**Cache musi działać bez Redis.** Listy klientów i urządzeń są cache'owane przez `HybridCache`: L1 w pamięci procesu, L2 w Redis. Redis przyspiesza odczyty, ale nie jest źródłem prawdy, więc jego awaria nie może zatrzymać API. Pusty connection string oznacza brak L2. Skonfigurowany, ale niedostępny Redis ma krótkie timeouty i opakowanie, które traktuje błąd jak brak wpisu, zamiast zwracać 500. Unieważnianie odbywa się przez tagi po każdym zapisie. Test integracyjny sprawdza działanie API przy wyłączonym Redis.

**Reguły narażone na współbieżność zabezpieczone także w bazie.** Sprawdzenie w kodzie nie wystarcza przy dwóch równoległych żądaniach, dlatego reguły 1, 3 i 6 mają odpowiednik w postaci indeksu, ograniczenia `CHECK` lub tokenu współbieżności, a naruszenie jest tłumaczone na `409`.

**Kontrakt API pilnowany w CI.** Dokument `openapi/v1.json` jest generowany przy buildzie i commitowany. CI odrzuca zmianę, jeśli wygenerowany dokument różni się od tego w repozytorium, więc każda zmiana kontraktu jest widoczna w review. API jest wersjonowane w ścieżce (`/api/v1`).

**Czas przez `TimeProvider`.** Kod nie odwołuje się do `DateTime.UtcNow`; testy reguły opóźnień i podsumowania dziennego sterują czasem przez `FakeTimeProvider`. Daty są przechowywane jako `timestamptz` w UTC, a strefa `Europe/Warsaw` jest używana tylko do prezentacji (e-mail, protokół PDF).

## Wdrożenie

```mermaid
flowchart LR
    push["push na main"] --> ci["GitHub Actions<br/>build, kontrola OpenAPI, testy"]
    ci --> ghcr["GHCR<br/>obraz latest i sha-commit"]
    ghcr --> hook["deploy hook Render<br/>imgURL = obraz commita"]
    hook --> render["Render Web Service<br/>Frankfurt"]
    render --> neon[("Neon PostgreSQL")]
    render --> upstash[("Upstash Redis, TLS")]
```

- Każdy push na `main` po zielonym buildzie i testach publikuje obraz `ghcr.io/mpalus-git/fixflow.api` z tagami `latest` i `sha-<commit>`, a następnie wywołuje deploy hook Render z adresem obrazu tego commita. Pull requesty tylko budują i testują.
- Usługa Render jest opisana w [render.yaml](render.yaml) i wdraża gotowy obraz z GHCR, a nie buduje go z Dockerfile. Sekrety (connection stringi, hasła kont demo) podaje się przy zakładaniu usługi; klucz JWT generuje Render.
- Baza to Neon z bezpośrednim connection stringiem (bez `-pooler`) i `SSL Mode=Require`, cache to Upstash Redis z TLS (`ssl=True`).
- Migracje wykonują się przy starcie aplikacji przez `MigrateAsync`.
- Render sprawdza `GET /health`, który nie dotyka bazy, więc health check nie wybudza uśpionej bazy Neon. `GET /health/ready` sprawdza połączenie z bazą.
- Za Renderem żądanie przechodzi przez Cloudflare i proxy Render, dlatego `X-Forwarded-For` ma trzy pozycje, a `ForwardedHeaders__ForwardLimit` wynosi `3`. Adres widziany przez aplikację (i przez limiter logowania) jest zapisywany w logu każdego żądania jako `ClientIp`.
- Brak sekretu `RENDER_DEPLOY_HOOK_URL` w repozytorium (np. w forku) nie psuje CI: krok wdrożenia jest pomijany z ostrzeżeniem.

## Ograniczenia

- **Usypianie na planie free Render.** Usługa zasypia po okresie bezczynności, a pierwsze żądanie po przerwie czeka na start kontenera. Joby Quartz działają tylko wtedy, gdy usługa nie śpi: flaga opóźnienia jest przeliczana przy starcie i co godzinę, ale podsumowanie o 7:00 i nocne usuwanie wygasłych refresh tokenów (o 3:00) mogą się w ogóle nie wykonać. Na stałym hostingu joby działają zgodnie z harmonogramem.
- **E-mail wyłączony w produkcji.** Darmowy plan Render blokuje wychodzący ruch SMTP, więc wdrożenie ma `Email__Enabled=false`, a zamiast wysyłki podsumowania dziennego w logu pojawia się tylko wpis o jej pominięciu. Lokalnie wiadomości widać w Mailpit.
- **Cache po awarii Redis.** Zmiana zapisana w czasie niedostępności Redis nie unieważnia wpisów L2. Po powrocie Redis lista klientów lub urządzeń może być nieaktualna maksymalnie przez czas życia wpisu, czyli 5 minut.
- **Łańcuch proxy zależy od infrastruktury Render.** Wartość `ForwardLimit` odpowiada obecnemu układowi Cloudflare i proxy Render. Jeśli Render go zmieni, limiter może zacząć rozpoznawać adresy błędnie; pole `ClientIp` w logach pozwala to szybko sprawdzić.
- **Blokada konta po nieudanych logowaniach.** Po 5 błędnych hasłach konto jest blokowane na 5 minut, więc ktoś znający e-mail może celowo zablokować cudze konto. To standardowy kompromis ASP.NET Core Identity; limit prób na adres IP ogranicza skalę takiego działania.
- **Publiczne konta demo.** Każdy może zalogować się jako Dispatcher lub Technician i zmieniać dane demonstracyjne; nie ma automatycznego resetu bazy.
- **Zdjęcia jako adresy URL.** Wpis serwisowy przechowuje listę adresów zdjęć, API nie przyjmuje plików.
- **Jedna waluta.** Ceny i sumy w protokole są w PLN.

## Co zrobiłbym inaczej

- **Upload zdjęć do blob storage.** Zamiast przyjmować gotowe adresy URL, API wydawałoby krótkotrwałe linki do bezpośredniego uploadu (np. S3 lub Azure Blob Storage) i zapisywało tylko klucze plików.
- **Joby poza procesem API.** Na hostingu, który usypia usługę, joby Quartz powinny działać w osobnym workerze albo być wyzwalane przez zewnętrzny scheduler wywołujący zabezpieczony endpoint.
- **Outbox dla e-maili.** Job zapisywałby wiadomość w tabeli w tej samej transakcji co dane, a osobny proces wysyłałby ją z ponawianiem. Dziś chwilowa niedostępność serwera SMTP oznacza utratę podsumowania z danego dnia.

## Licencja

Wszelkie prawa zastrzeżone. Kod jest udostępniony wyłącznie do wglądu; wykorzystanie, kopiowanie lub modyfikacja wymagają zgody autora.
