# Analitika cijena blagajničkih podataka

Web aplikacija koja obrtima na temelju tržišnog korpusa blagajničkih podataka procjenjuje je
li cijena njihove usluge preniska, primjerena ili previsoka, te predlaže raspon cijene.

Završni rad, stručni prijediplomski studij Informacijske tehnologije, Sveučilište u Zadru.

## Kako radi

Korisnik unese uslugu i cijenu (npr. "muško šišanje", 9 €). Sustav naziv normalizira, spari ga
s kanonskom uslugom, izračuna statistiku nad usporedivim tržišnim cijenama i vrati procjenu s
preporučenim rasponom i pozicijom u percentilima.

Preporuka je usidrena na **medijan i interkvartilni raspon**, a ne na aritmetičku sredinu, jer
su cijene u stvarnim podacima jako iskošene. Ekstremne vrijednosti uklanjaju se Tukeyjevim
ogradama (1,5 × IQR) prije računanja.

## Tehnologije

| Sloj | Tehnologija |
|---|---|
| Frontend | Angular 21, Chart.js 4 |
| Backend | ASP.NET Core Web API (.NET 9) |
| ORM | Entity Framework Core |
| Baza | SQL Server Express |
| Autentikacija | JWT (HMAC-SHA256), BCrypt |
| API dokumentacija | Scalar (OpenAPI) |

## Struktura

Backend prati Clean Architecture s pravilom ovisnosti prema unutra:

```
src/PricingAnalytics.Domain          Entiteti i domenski servisi: normalizacija naziva,
                                     sparivanje, statistika, preporuka. Bez ijedne
                                     infrastrukturne ovisnosti - ovo je jezgra projekta.
src/PricingAnalytics.Application     Use case sloj: portovi (interfaceovi) i servisi koji
                                     orkestriraju domenu. Ovisi samo o Domain.
src/PricingAnalytics.Infrastructure  EF Core, repozitoriji, CSV čitač, BCrypt, JWT, migracije.
src/PricingAnalytics.Api             Web API, kontroleri, composition root.
tools/PricingAnalytics.Explorer      Konzolni alat za inspekciju korpusa.
frontend/                            Angular SPA.
```

## Preduvjeti

- .NET 9 SDK
- Node.js 22+
- SQL Server Express (instanca `SQLEXPRESS`, Windows Authentication)

## Podaci

Tržišni korpus je stvarni izvoz iz blagajničkog sustava i **nije uključen u repozitorij**.

Za pokretanje je potrebna datoteka `data/prices.csv` sa sljedećom strukturom:

```csv
"Name","Price"
"Muško šišanje","10.00"
"Žensko šišanje","14.00"
```

Bez te datoteke aplikacija se pokreće normalno, ali tablica `MarketEntries` ostaje prazna, pa
analiza nema s čim usporediti unesenu cijenu.

## Pokretanje

Baza se kreira i puni automatski pri prvom pokretanju API-ja u Development modu (migracije se
primjenjuju, korpus se učitava iz CSV-a; ponovna pokretanja ne diraju već napunjenu bazu).

Dva terminala:

```bash
# 1. Backend - http://localhost:5049, Scalar na /scalar
dotnet run --project src/PricingAnalytics.Api

# 2. Frontend - http://localhost:4200
cd frontend && npx ng serve
```

## Inspekcija korpusa

Konzolni alat ispisuje raspodjelu po kanonskim uslugama, statistiku i najčešće nesparene nazive:

```bash
dotnet run --project tools/PricingAnalytics.Explorer
```

## Napomena o konfiguraciji

JWT ključ u `appsettings.json` je razvojni placeholder. Aplikacija je namijenjena lokalnom
radu; prije bilo kakvog postavljanja u produkciju ključ mora doći iz korisničkih tajni ili
varijabli okruženja.
