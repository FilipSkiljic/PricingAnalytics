/* ============================================================================
   Statistika trzisnog korpusa (tablica MarketEntries)
   Pokrenuti u SSMS-u nad bazom BlagajnaAnalitika.

   VAZNA NAPOMENA
   --------------
   Ovi upiti daju brojke o korpusu KAO CJELINI. Oni NE mogu reproducirati
   statistiku po pojedinoj usluzi (npr. "medijan za musko sisanje"), jer
   sparivanje naziva s kanonskom uslugom radi normalizacija i matching u
   C# kodu (PricingAnalytics.Domain), a ne SQL.

   Razlog: isti pojam je u podacima zapisan na desetke nacina, ukljucujuci
   nedosljedne dijakritike - "sisanje", "sišanje", "šisanje", "šišanje".
   SQL LIKE bi morao rucno nabrajati svaku kombinaciju, i to samo za jednu
   rijec, bez padeza i bez redoslijeda rijeci.

   Za statistiku po usluzi koristiti:
     - aplikaciju (kartica rezultata pokazuje medijan, Q1-Q3, uzorak), ili
     - dotnet run --project tools/PricingAnalytics.Explorer
   ============================================================================ */


/* ---------------------------------------------------------------------------
   1. Osnovni opseg korpusa
   --------------------------------------------------------------------------- */
SELECT
    COUNT(*)                                   AS ukupno_redaka,
    COUNT(DISTINCT Name)                       AS jedinstvenih_naziva,
    CAST(MIN(Price) AS DECIMAL(10,2))          AS najniza_cijena,
    CAST(MAX(Price) AS DECIMAL(10,2))          AS najvisa_cijena,
    CAST(AVG(Price) AS DECIMAL(10,2))          AS prosjecna_cijena,
    SUM(CASE WHEN Price = 0 THEN 1 ELSE 0 END) AS cijena_nula,
    SUM(CASE WHEN Price < 1 THEN 1 ELSE 0 END) AS cijena_ispod_1_eur
FROM MarketEntries;


/* ---------------------------------------------------------------------------
   2. Kvartili nad cijelim korpusom

   Pokazuje koliko je distribucija iskosena: prosjek je osjetno visi od
   medijana, sto je argument za sidrenje preporuke na medijan.
   --------------------------------------------------------------------------- */
SELECT DISTINCT
    CAST(PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY Price) OVER () AS DECIMAL(10,2)) AS Q1,
    CAST(PERCENTILE_CONT(0.50) WITHIN GROUP (ORDER BY Price) OVER () AS DECIMAL(10,2)) AS medijan,
    CAST(PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY Price) OVER () AS DECIMAL(10,2)) AS Q3,
    CAST(AVG(Price) AS DECIMAL(10,2))                                                  AS prosjek
FROM MarketEntries;


/* ---------------------------------------------------------------------------
   3. Najcesce ponavljani nazivi

   Isti naziv se pojavljuje kod vise obrta - to su usluge s najvecim uzorkom.
   --------------------------------------------------------------------------- */
SELECT TOP 20
    Name        AS naziv,
    COUNT(*)    AS broj_obrta,
    CAST(MIN(Price) AS DECIMAL(10,2)) AS najniza,
    CAST(MAX(Price) AS DECIMAL(10,2)) AS najvisa
FROM MarketEntries
GROUP BY Name
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;


/* ---------------------------------------------------------------------------
   4. Distribucija cijena po razredima

   Podloga za tvrdnju da je distribucija desno iskosena (dug rep prema vise).
   --------------------------------------------------------------------------- */
SELECT razred, COUNT(*) AS broj_unosa
FROM (
    SELECT CASE
        WHEN Price < 5   THEN '1. ispod 5 EUR'
        WHEN Price < 15  THEN '2. od 5 do 15 EUR'
        WHEN Price < 30  THEN '3. od 15 do 30 EUR'
        WHEN Price < 60  THEN '4. od 30 do 60 EUR'
        WHEN Price < 100 THEN '5. od 60 do 100 EUR'
        ELSE                  '6. iznad 100 EUR'
    END AS razred
    FROM MarketEntries
) t
GROUP BY razred
ORDER BY razred;


/* ---------------------------------------------------------------------------
   5. DOKAZ NEDOSLJEDNOSTI U PODACIMA

   Ovo je najkorisniji upit za rad: pokazuje da je ista rijec zapisana s
   razlicitim kombinacijama dijakritika. Upravo zato je normalizacija naziva
   nuzna, i zato SQL sam ne moze zamijeniti engine.
   --------------------------------------------------------------------------- */
SELECT N'šišanje (oba slova s kvacicom)' AS oblik_zapisa,
       COUNT(*) AS broj_unosa
FROM MarketEntries WHERE Name COLLATE Latin1_General_CS_AS LIKE N'%šišanj%'
UNION ALL
SELECT N'sisanje (bez kvacica)',
       COUNT(*)
FROM MarketEntries WHERE Name COLLATE Latin1_General_CS_AS LIKE N'%sisanj%'
UNION ALL
SELECT N'sišanje (kvacica samo na drugom)',
       COUNT(*)
FROM MarketEntries WHERE Name COLLATE Latin1_General_CS_AS LIKE N'%sišanj%'
UNION ALL
SELECT N'šisanje (kvacica samo na prvom)',
       COUNT(*)
FROM MarketEntries WHERE Name COLLATE Latin1_General_CS_AS LIKE N'%šisanj%';


/* ---------------------------------------------------------------------------
   6. Ucinak osnovnog ciscenja naziva

   Baza je u CI_AS collationu, pa COUNT(DISTINCT Name) vec ignorira velika i
   mala slova. Da bi se vidjelo koliko naziva se razlikuje SAMO po velicini
   slova, usporedba mora biti binarna (BIN2).

   Razlika izmedu prva dva stupca je broj naziva koji nestaju vec pukim
   svodenjem na mala slova - prvi i najjednostavniji korak normalizacije.
   --------------------------------------------------------------------------- */
SELECT
    COUNT(DISTINCT Name COLLATE Latin1_General_BIN2)                      AS doslovno_razlicitih,
    COUNT(DISTINCT LOWER(Name) COLLATE Latin1_General_BIN2)               AS nakon_malih_slova,
    COUNT(DISTINCT LOWER(LTRIM(RTRIM(Name))) COLLATE Latin1_General_BIN2) AS nakon_malih_slova_i_trima
FROM MarketEntries;


/* ---------------------------------------------------------------------------
   7. Korisnicki podaci - popis registriranih obrta i velicina njihovih cjenika
   --------------------------------------------------------------------------- */
SELECT
    b.Id,
    b.Name  AS obrt,
    b.Email,
    COUNT(s.Id) AS broj_usluga_u_cjeniku
FROM Businesses b
LEFT JOIN Services s ON s.BusinessId = b.Id
GROUP BY b.Id, b.Name, b.Email
ORDER BY b.Id;
