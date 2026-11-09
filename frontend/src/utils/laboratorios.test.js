import { test } from 'node:test';
import assert from 'node:assert/strict';
import { esLaboratorioHabilitado, filtrarLaboratoriosHabilitados } from './laboratorios.js';

test('solo el estado canonico Habilitado permite seleccionar un laboratorio', () => {
    for (const estado of ['Fuera de servicio', 'habilitado', ' Habilitado ', '', null]) {
        assert.equal(esLaboratorioHabilitado({ estado }), false);
    }
    assert.equal(esLaboratorioHabilitado(null), false);
    assert.equal(esLaboratorioHabilitado({ estado: 'Habilitado' }), true);
});

test('filtra sin mutar el catalogo y conserva orden e identidad de los habilitados', () => {
    const lista = [{ id: 3, estado: 'Habilitado' }, { id: 2, estado: 'Fuera de servicio' }, { id: 1, estado: 'Habilitado' }];
    const filtrados = filtrarLaboratoriosHabilitados(lista);
    assert.deepEqual(filtrados.map(l => l.id), [3, 1]);
    assert.equal(filtrados[0], lista[0]);
    assert.equal(lista.length, 3);
    assert.deepEqual(filtrarLaboratoriosHabilitados(undefined), []);
});
