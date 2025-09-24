--
-- PostgreSQL database dump
--

\restrict AefIis3z1wrNccLNoO5969UDQzSPB6yPe1da7lbbfaPAnftOxIqxJNYKhz5Vqpz

-- Dumped from database version 17.6
-- Dumped by pg_dump version 17.6

-- Started on 2025-09-24 15:09:06

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 218 (class 1259 OID 16390)
-- Name: Users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Users" (
    "Id" integer NOT NULL,
    "Username" character varying(100) NOT NULL,
    "PasswordHash" text NOT NULL
);


ALTER TABLE public."Users" OWNER TO postgres;

--
-- TOC entry 217 (class 1259 OID 16389)
-- Name: Users_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Users_Id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Users_Id_seq" OWNER TO postgres;

--
-- TOC entry 4798 (class 0 OID 0)
-- Dependencies: 217
-- Name: Users_Id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Users_Id_seq" OWNED BY public."Users"."Id";


--
-- TOC entry 4641 (class 2604 OID 16393)
-- Name: Users Id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users" ALTER COLUMN "Id" SET DEFAULT nextval('public."Users_Id_seq"'::regclass);


--
-- TOC entry 4792 (class 0 OID 16390)
-- Dependencies: 218
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Users" ("Id", "Username", "PasswordHash") FROM stdin;
1	user	$2a$11$pUSFRpoP35lMrMyaek81kOsl7AJqFrs/sDKTbuw3NJn9sVEMbJ8VC
2	admin	$2a$11$F7ug7Ysh7YNOZHjmOhpoEuPVFYZi9.u25LBCDEiz55DzpTKU9iKZe
\.


--
-- TOC entry 4799 (class 0 OID 0)
-- Dependencies: 217
-- Name: Users_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Users_Id_seq"', 2, true);


--
-- TOC entry 4643 (class 2606 OID 16399)
-- Name: Users Users_Username_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_Username_key" UNIQUE ("Username");


--
-- TOC entry 4645 (class 2606 OID 16397)
-- Name: Users Users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_pkey" PRIMARY KEY ("Id");


-- Completed on 2025-09-24 15:09:06

--
-- PostgreSQL database dump complete
--

\unrestrict AefIis3z1wrNccLNoO5969UDQzSPB6yPe1da7lbbfaPAnftOxIqxJNYKhz5Vqpz

