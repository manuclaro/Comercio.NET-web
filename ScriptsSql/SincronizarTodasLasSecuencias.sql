-- ================================================================================
-- Script para Sincronizar TODAS las Secuencias de la Base de Datos
-- ================================================================================
-- 
-- PROBLEMA:
-- Error: 23505: llave duplicada viola restricción de unicidad
--
-- CAUSA:
-- Las secuencias están desincronizadas con los máximos IDs actuales 
-- de sus respectivas tablas.
--
-- SOLUCIÓN:
-- Este script sincroniza automáticamente TODAS las secuencias de la base de datos
-- con los máximos IDs de sus tablas correspondientes.
--
-- ================================================================================

DO $$
DECLARE
    seq_record RECORD;
    tabla_nombre TEXT;
    columna_nombre TEXT;
    max_valor BIGINT;
    query_text TEXT;
BEGIN
    -- Recorrer todas las secuencias de la base de datos
    FOR seq_record IN 
        SELECT 
            sequence_schema,
            sequence_name,
            -- Intentar extraer el nombre de la tabla y columna del nombre de la secuencia
            -- Formato típico: tabla_columna_seq
            CASE 
                WHEN sequence_name LIKE '%_seq' THEN 
                    regexp_replace(sequence_name, '_seq$', '')
                ELSE sequence_name
            END as base_name
        FROM information_schema.sequences
        WHERE sequence_schema NOT IN ('pg_catalog', 'information_schema')
    LOOP
        BEGIN
            -- Intentar encontrar la tabla y columna correspondiente
            -- PostgreSQL usa el formato: tabla_columna_seq

            -- Extraer nombre de tabla (todo menos la última parte después del último _)
            tabla_nombre := regexp_replace(seq_record.base_name, '_[^_]+$', '');

            -- Extraer nombre de columna (la parte después del último _)
            columna_nombre := regexp_replace(seq_record.base_name, '^.*_', '');

            -- Verificar si la tabla y columna existen
            EXECUTE format('SELECT MAX(%I) FROM %I', columna_nombre, tabla_nombre) INTO max_valor;

            -- Si encontró un valor, sincronizar la secuencia
            IF max_valor IS NOT NULL THEN
                EXECUTE format('SELECT setval(%L, %s, true)', 
                    seq_record.sequence_schema || '.' || seq_record.sequence_name, 
                    COALESCE(max_valor, 1));

                RAISE NOTICE '? Secuencia sincronizada: % ? Valor: %', 
                    seq_record.sequence_name, COALESCE(max_valor, 1);
            ELSE
                -- Si la tabla está vacía, resetear a 1
                EXECUTE format('SELECT setval(%L, 1, false)', 
                    seq_record.sequence_schema || '.' || seq_record.sequence_name);

                RAISE NOTICE '? Secuencia reseteada (tabla vacía): % ? Valor: 1', 
                    seq_record.sequence_name;
            END IF;

        EXCEPTION 
            WHEN OTHERS THEN
                -- Si hay error (tabla/columna no existe), intentar con nombres alternativos comunes
                BEGIN
                    -- Caso especial: idfactura en lugar de id
                    IF seq_record.base_name LIKE '%_idfactura' THEN
                        tabla_nombre := regexp_replace(seq_record.base_name, '_idfactura$', '');
                        EXECUTE format('SELECT MAX(idfactura) FROM %I', tabla_nombre) INTO max_valor;

                        EXECUTE format('SELECT setval(%L, %s, true)', 
                            seq_record.sequence_schema || '.' || seq_record.sequence_name, 
                            COALESCE(max_valor, 1));

                        RAISE NOTICE '? Secuencia sincronizada (idfactura): % ? Valor: %', 
                            seq_record.sequence_name, COALESCE(max_valor, 1);
                    ELSE
                        RAISE NOTICE '??  No se pudo sincronizar: % (tabla/columna no encontrada)', 
                            seq_record.sequence_name;
                    END IF;
                EXCEPTION
                    WHEN OTHERS THEN
                        RAISE NOTICE '??  Error al sincronizar: % - %', 
                            seq_record.sequence_name, SQLERRM;
                END;
        END;
    END LOOP;

    RAISE NOTICE '========================================';
    RAISE NOTICE '? Sincronización completada';
    RAISE NOTICE '========================================';
END $$;

-- ================================================================================
-- Verificar todas las secuencias (opcional)
-- ================================================================================

SELECT 
    sequence_name AS secuencia,
    last_value AS valor_actual
FROM 
    information_schema.sequences s
    LEFT JOIN pg_sequences ps ON s.sequence_name = ps.sequencename
WHERE 
    s.sequence_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY 
    sequence_name;
