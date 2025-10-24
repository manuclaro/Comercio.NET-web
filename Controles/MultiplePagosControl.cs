using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Comercio.NET.Controles
{
    public partial class MultiplePagosControl : UserControl
    {
        // Estructura para representar un pago
        public class DetallePago
        {
            public string MedioPago { get; set; }
            public decimal Importe { get; set; }
            public string Observaciones { get; set; }
            public DateTime Fecha { get; set; } = DateTime.Now;
        }

        // Propiedades públicas
        public decimal ImporteTotal { get; set; }
        public decimal ImportePendiente => ImporteTotal - ImporteAsignado;
        public decimal ImporteAsignado => _pagos.Sum(p => p.Importe);
        public bool PagoCompleto => Math.Abs(ImportePendiente) < 0.01m;
        public List<DetallePago> Pagos => _pagos.ToList();

        // Controles de interfaz
        private ComboBox cmbMedioPago;
        private NumericUpDown nudImporte;
        private TextBox txtObservaciones;
        private Button btnAgregar;
        private Button btnEliminar;
        private DataGridView dgvPagos;
        private Label lblTotal;
        private Label lblAsignado;
        private Label lblPendiente;
        private ProgressBar progressBar;

        // NUEVO: Botones de acceso rápido
        private Button btnMitad;
        private Button btnTercio;
        private Button btnCompleto;
        private Button btnMil;
        private Button btnDosMil;
        private Button btnCincoMil;

        // Datos internos
        private List<DetallePago> _pagos;
        // MODIFICADO: Solo 3 medios de pago
        private readonly string[] _mediosPago = { "Efectivo", "DNI", "MercadoPago" };

        public MultiplePagosControl()
        {
            _pagos = new List<DetallePago>();
            InitializeComponent();
            ConfigurarEventos();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuración general
            this.Size = new Size(600, 400);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            // Panel superior - Controles de entrada - COMPACTADO para dar más espacio a la grilla
            var panelEntrada = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110, // REDUCIDO de 140 a 110 para dar más espacio a la grilla
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Medio de pago - COMPACTADO
            var lblMedio = new Label
            {
                Text = "Medio de pago:",
                Location = new Point(10, 8), // REDUCIDO de 15 a 8
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) // REDUCIDO ligeramente
            };

            cmbMedioPago = new ComboBox
            {
                Location = new Point(10, 25), // REDUCIDO de 35 a 25
                Width = 110, // REDUCIDO de 120 a 110
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5F) // REDUCIDO ligeramente
            };
            cmbMedioPago.Items.AddRange(_mediosPago);
            cmbMedioPago.SelectedIndex = 0;

            // Importe - COMPACTADO
            var lblImporte = new Label
            {
                Text = "Importe:",
                Location = new Point(130, 8), // AJUSTADO posición
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };

            nudImporte = new NumericUpDown
            {
                Location = new Point(130, 25), // AJUSTADO posición
                Width = 90, // REDUCIDO de 100 a 90
                Maximum = 999999999,
                DecimalPlaces = 0,
                Font = new Font("Segoe UI", 8.5F),
                ThousandsSeparator = true
            };

            // Observaciones - COMPACTADO
            var lblObs = new Label
            {
                Text = "Observaciones:",
                Location = new Point(230, 8), // AJUSTADO posición
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };

            txtObservaciones = new TextBox
            {
                Location = new Point(230, 25), // AJUSTADO posición
                Width = 130, // REDUCIDO de 150 a 130
                Font = new Font("Segoe UI", 8.5F),
                PlaceholderText = "Opcional..."
            };

            // Botones principales - COMPACTADOS
            btnAgregar = new Button
            {
                Text = "+ Agregar",
                Location = new Point(370, 23), // AJUSTADO posición
                Size = new Size(75, 23), // REDUCIDO tamaño
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold) // REDUCIDO fuente
            };

            btnEliminar = new Button
            {
                Text = "- Quitar",
                Location = new Point(450, 23), // AJUSTADO posición
                Size = new Size(60, 23), // REDUCIDO tamaño
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold)
            };

            // NUEVO: Etiqueta para botones rápidos - COMPACTADA
            var lblRapidos = new Label
            {
                Text = "Accesos rápidos:",
                Location = new Point(10, 50), // REDUCIDO de 70 a 50
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold), // REDUCIDO ligeramente
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // NUEVO: Botones de acceso rápido - COMPACTADOS
            btnMitad = new Button
            {
                Text = "Mitad",
                Location = new Point(10, 68), // REDUCIDO de 90 a 68
                Size = new Size(50, 22), // REDUCIDO tamaño
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold) // REDUCIDO fuente
            };

            btnTercio = new Button
            {
                Text = "1/3",
                Location = new Point(65, 68), // AJUSTADO posición
                Size = new Size(35, 22), // REDUCIDO tamaño
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold)
            };

            btnCompleto = new Button
            {
                Text = "Completo",
                Location = new Point(105, 68), // AJUSTADO posición
                Size = new Size(60, 22), // REDUCIDO tamaño
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold)
            };

            // NUEVO: Botones de valores fijos - COMPACTADOS
            btnMil = new Button
            {
                Text = "$1.000",
                Location = new Point(170, 68), // AJUSTADO posición
                Size = new Size(50, 22), // REDUCIDO tamaño
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold)
            };

            btnDosMil = new Button
            {
                Text = "$2.000",
                Location = new Point(225, 68), // AJUSTADO posición
                Size = new Size(50, 22), // REDUCIDO tamaño
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold)
            };

            btnCincoMil = new Button
            {
                Text = "$5.000",
                Location = new Point(280, 68), // AJUSTADO posición
                Size = new Size(50, 22), // REDUCIDO tamaño
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold)
            };

            // Botón para completar automáticamente con efectivo - COMPACTADO
            var btnCompletar = new Button
            {
                Text = "Completar con Efectivo",
                Location = new Point(340, 68), // AJUSTADO posición
                Size = new Size(140, 22), // REDUCIDO tamaño
                BackColor = Color.FromArgb(23, 162, 184),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F, FontStyle.Bold)
            };
            btnCompletar.Click += BtnCompletar_Click;

            // Agregar controles al panel
            panelEntrada.Controls.AddRange(new Control[] {
                lblMedio, cmbMedioPago, lblImporte, nudImporte,
                lblObs, txtObservaciones, btnAgregar, btnEliminar,
                lblRapidos, btnMitad, btnTercio, btnCompleto,
                btnMil, btnDosMil, btnCincoMil, btnCompletar
            });

            // Panel de totales - COMPACTADO
            var panelTotales = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70, // REDUCIDO de 80 a 70
                BackColor = Color.FromArgb(0, 120, 215),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblTotal = new Label
            {
                Text = "Total: $0,00",
                Location = new Point(15, 10), // REDUCIDO de 15 a 10
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold) // REDUCIDO ligeramente
            };

            lblAsignado = new Label
            {
                Text = "Asignado: $0,00",
                Location = new Point(15, 28), // REDUCIDO de 35 a 28
                AutoSize = true,
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) // REDUCIDO ligeramente
            };

            lblPendiente = new Label
            {
                Text = "Pendiente: $0,00",
                Location = new Point(15, 46), // REDUCIDO de 55 a 46
                AutoSize = true,
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) // REDUCIDO ligeramente
            };

            progressBar = new ProgressBar
            {
                Location = new Point(200, 20), // AJUSTADO posición
                Size = new Size(300, 25), // REDUCIDO altura de 30 a 25
                Style = ProgressBarStyle.Continuous,
                BackColor = Color.White
            };

            panelTotales.Controls.AddRange(new Control[] { lblTotal, lblAsignado, lblPendiente, progressBar });

            // DataGridView - AHORA TIENE MÁS ESPACIO (aprox. 30px más)
            dgvPagos = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 8.5F), // REDUCIDO ligeramente para ver más filas
                RowTemplate = { Height = 22 } // REDUCIDO altura de filas para ver más
            };

            ConfigurarDataGridView();

            // Agregar controles al UserControl
            this.Controls.Add(dgvPagos);
            this.Controls.Add(panelTotales);
            this.Controls.Add(panelEntrada);

            this.ResumeLayout(false);
        }


        private void ConfigurarDataGridView()
        {
            dgvPagos.Columns.Clear();
            dgvPagos.Columns.Add("MedioPago", "Medio de Pago");
            dgvPagos.Columns.Add("Importe", "Importe");
            dgvPagos.Columns.Add("Observaciones", "Observaciones");
            dgvPagos.Columns.Add("Fecha", "Fecha/Hora");

            // Configurar formato de columnas - MODIFICADO para números enteros
            dgvPagos.Columns["Importe"].DefaultCellStyle.Format = "N0"; // CAMBIADO: Sin decimales
            dgvPagos.Columns["Importe"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvPagos.Columns["Fecha"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            dgvPagos.Columns["Fecha"].Width = 120;
            dgvPagos.Columns["MedioPago"].Width = 100;
            dgvPagos.Columns["Importe"].Width = 100;

            // Estilos
            dgvPagos.EnableHeadersVisualStyles = false;
            dgvPagos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 58, 64);
            dgvPagos.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPagos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }

        private void ConfigurarEventos()
        {
            btnAgregar.Click += BtnAgregar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            
            // Eventos para botones de acceso rápido
            btnMitad.Click += (s, e) => EstablecerImporte(ImportePendiente / 2);
            btnTercio.Click += (s, e) => EstablecerImporte(ImportePendiente / 3);
            btnCompleto.Click += (s, e) => EstablecerImporte(ImportePendiente);
            btnMil.Click += (s, e) => EstablecerImporte(1000);
            btnDosMil.Click += (s, e) => EstablecerImporte(2000);
            btnCincoMil.Click += (s, e) => EstablecerImporte(5000);
            
            // NUEVO: Agregar eventos adicionales para mejorar la experiencia del usuario
            nudImporte.ValueChanged += (s, e) => ActualizarEstadoBotones();
            cmbMedioPago.SelectedIndexChanged += (s, e) => ActualizarEstadoBotones();
            
            nudImporte.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnAgregar_Click(s, e);
                }
            };
        }

        // NUEVO: Método para establecer importe con redondeo
        private void EstablecerImporte(decimal importe)
        {
            // Redondear a número entero
            decimal importeRedondeado = Math.Round(importe, 0);
            
            // Verificar que no exceda el máximo del control
            if (importeRedondeado > nudImporte.Maximum)
                importeRedondeado = nudImporte.Maximum;
            
            // Verificar que no sea negativo
            if (importeRedondeado < 0)
                importeRedondeado = 0;
            
            nudImporte.Value = importeRedondeado;
            nudImporte.Focus();
            
            System.Diagnostics.Debug.WriteLine($"?? Importe establecido: {importeRedondeado:N0}");
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== BOTÓN AGREGAR CLICKEADO ===");
                System.Diagnostics.Debug.WriteLine($"Medio de pago: {cmbMedioPago.SelectedItem}");
                System.Diagnostics.Debug.WriteLine($"Importe: {nudImporte.Value}");
                System.Diagnostics.Debug.WriteLine($"Importe Total: {ImporteTotal}");
                System.Diagnostics.Debug.WriteLine($"Importe Asignado: {ImporteAsignado}");
                System.Diagnostics.Debug.WriteLine($"Importe Pendiente: {ImportePendiente}");

                if (ValidarEntrada())
                {
                    var pago = new DetallePago
                    {
                        MedioPago = cmbMedioPago.SelectedItem.ToString(),
                        Importe = nudImporte.Value, // Ya es número entero
                        Observaciones = txtObservaciones.Text.Trim()
                    };

                    _pagos.Add(pago);
                    System.Diagnostics.Debug.WriteLine($"? Pago agregado: {pago.MedioPago} - ${pago.Importe:N0}");
                    
                    ActualizarVista();
                    LimpiarEntrada();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? Validación falló");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error en BtnAgregar_Click: {ex.Message}");
                MessageBox.Show($"Error al agregar pago: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvPagos.SelectedRows.Count > 0)
            {
                int index = dgvPagos.SelectedRows[0].Index;
                if (index >= 0 && index < _pagos.Count)
                {
                    var pago = _pagos[index];
                    var resultado = MessageBox.Show(
                        $"¿Eliminar el pago de ${pago.Importe:N0} por {pago.MedioPago}?",
                        "Confirmar eliminación",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (resultado == DialogResult.Yes)
                    {
                        _pagos.RemoveAt(index);
                        ActualizarVista();
                    }
                }
            }
        }

        private void BtnCompletar_Click(object sender, EventArgs e)
        {
            if (ImportePendiente > 0)
            {
                // Redondear a número entero
                decimal importeCompleto = Math.Round(ImportePendiente, 0);
                
                var resultado = MessageBox.Show(
                    $"¿Completar el pago pendiente de ${importeCompleto:N0} con Efectivo?",
                    "Completar pago",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resultado == DialogResult.Yes)
                {
                    var pago = new DetallePago
                    {
                        MedioPago = "Efectivo",
                        Importe = importeCompleto,
                        Observaciones = "Completado automáticamente"
                    };

                    _pagos.Add(pago);
                    ActualizarVista();
                }
            }
        }

        private bool ValidarEntrada()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== VALIDANDO ENTRADA ===");
                
                if (cmbMedioPago.SelectedItem == null)
                {
                    System.Diagnostics.Debug.WriteLine("? No hay medio de pago seleccionado");
                    MessageBox.Show("Seleccione un medio de pago.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbMedioPago.Focus();
                    return false;
                }

                if (nudImporte.Value <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("? Importe debe ser mayor a cero");
                    MessageBox.Show("El importe debe ser mayor a cero.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    nudImporte.Focus();
                    return false;
                }

                // CORREGIDO: Permitir pagos que excedan ligeramente el total (diferencia mínima)
                decimal importeAExceder = ImporteAsignado + nudImporte.Value - ImporteTotal;
                if (importeAExceder > 1m) // Permitir hasta $1 de diferencia para números enteros
                {
                    System.Diagnostics.Debug.WriteLine($"? Importe excede el pendiente. Exceso: ${importeAExceder:N0}");
                    MessageBox.Show(
                        $"El importe ingresado (${nudImporte.Value:N0}) excede el pendiente (${ImportePendiente:N0}).\n" +
                        $"Exceso: ${importeAExceder:N0}",
                        "Validación",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    nudImporte.Focus();
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("? Validación exitosa");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error en validación: {ex.Message}");
                MessageBox.Show($"Error en validación: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void LimpiarEntrada()
        {
            // CORREGIDO: Establecer un valor por defecto más inteligente para números enteros
            if (ImportePendiente > 0)
            {
                decimal valorSugerido = Math.Round(ImportePendiente, 0);
                nudImporte.Value = Math.Min(valorSugerido, nudImporte.Maximum);
            }
            else
            {
                nudImporte.Value = 0;
            }
            
            txtObservaciones.Text = "";
            cmbMedioPago.Focus();
        }

        // NUEVO: Método para actualizar el estado de los botones
        private void ActualizarEstadoBotones()
        {
            try
            {
                // El botón agregar debe estar habilitado si:
                // 1. Hay un medio de pago seleccionado
                // 2. El importe es mayor a 0
                // 3. No se excede significativamente el total
                bool puedeAgregar = cmbMedioPago.SelectedItem != null && 
                                   nudImporte.Value > 0 && 
                                   (ImporteAsignado + nudImporte.Value - ImporteTotal) <= 1m; // Tolerancia de $1

                btnAgregar.Enabled = puedeAgregar;
                
                // Cambiar color del botón según estado
                if (puedeAgregar)
                {
                    btnAgregar.BackColor = Color.FromArgb(40, 167, 69);
                    btnAgregar.ForeColor = Color.White;
                }
                else
                {
                    btnAgregar.BackColor = Color.LightGray;
                    btnAgregar.ForeColor = Color.DarkGray;
                }

                // Actualizar estado de botones rápidos
                bool hayImportePendiente = ImportePendiente > 0;
                btnMitad.Enabled = hayImportePendiente;
                btnTercio.Enabled = hayImportePendiente;
                btnCompleto.Enabled = hayImportePendiente;

                System.Diagnostics.Debug.WriteLine($"?? Estado botón Agregar: {(puedeAgregar ? "Habilitado" : "Deshabilitado")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando botones: {ex.Message}");
            }
        }

        public void EstablecerImporteTotal(decimal total)
        {
            ImporteTotal = total;
            LimpiarPagos(); // LimpiarPagos ya llama a ActualizarVista()

            // Sugerir el total como primer importe (redondeado)
            if (total > 0)
            {
                decimal totalRedondeado = Math.Round(total, 0);
                nudImporte.Value = Math.Min(totalRedondeado, nudImporte.Maximum);
            }

            // Actualizar estado de botones (pero no volver a forzar ActualizarVista)
            ActualizarEstadoBotones();
        }

        public void LimpiarPagos()
        {
            _pagos.Clear();
            ActualizarVista();
        }

        private void ActualizarVista()
        {
            try
            {
                // Actualizar DataGridView
                dgvPagos.Rows.Clear();
                foreach (var pago in _pagos)
                {
                    dgvPagos.Rows.Add(pago.MedioPago, pago.Importe, pago.Observaciones, pago.Fecha);
                }

                // Actualizar labels
                lblTotal.Text = $"Total: ${ImporteTotal:N0}";
                lblAsignado.Text = $"Asignado: ${ImporteAsignado:N0}";
                lblPendiente.Text = $"Pendiente: ${ImportePendiente:N0}";

                if (PagoCompleto)
                {
                    lblPendiente.ForeColor = Color.LightGreen;
                    lblPendiente.Text = "PAGO COMPLETO";
                }
                else if (ImporteAsignado > ImporteTotal)
                {
                    lblPendiente.ForeColor = Color.Red;
                    lblPendiente.Text = $"EXCESO: ${Math.Abs(ImportePendiente):N0}";
                }
                else
                {
                    lblPendiente.ForeColor = Color.Yellow;
                    lblPendiente.Text = $"PENDIENTE: ${ImportePendiente:N0}";
                }

                // Progress bar seguro
                if (ImporteTotal > 0)
                {
                    var porcentaje = (int)Math.Min(100, (ImporteAsignado / ImporteTotal) * 100);
                    progressBar.Value = porcentaje;
                }

                btnEliminar.Enabled = _pagos.Count > 0;
                ActualizarEstadoBotones();

                // Disparar evento de cambio de forma asíncrona para evitar reentradas
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() => OnPagosChanged?.Invoke(this, EventArgs.Empty)));
                }
                else
                {
                    OnPagosChanged?.Invoke(this, EventArgs.Empty);
                }

                System.Diagnostics.Debug.WriteLine($"?? Vista actualizada - Pagos: {_pagos.Count}, Total: ${ImporteTotal:N0}, Asignado: ${ImporteAsignado:N0}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando vista: {ex.Message}");
            }
        }

        // Evento para notificar cambios
        public event EventHandler OnPagosChanged;

        // Métodos públicos de utilidad
        public bool TienePagoDigital()
        {
            return _pagos.Any(p => p.MedioPago == "DNI" || p.MedioPago == "MercadoPago");
        }

        public bool TieneSoloEfectivo()
        {
            return _pagos.All(p => p.MedioPago == "Efectivo");
        }

        public string ObtenerResumenPagos()
        {
            if (!_pagos.Any())
                return "Sin pagos registrados";

            var grupos = _pagos.GroupBy(p => p.MedioPago)
                               .Select(g => $"{g.Key}: ${g.Sum(x => x.Importe):N0}") // MODIFICADO: Sin decimales
                               .ToArray();

            return string.Join(" | ", grupos);
        }

        public Dictionary<string, decimal> ObtenerPagosPorMedio()
        {
            return _pagos.GroupBy(p => p.MedioPago)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Importe));
        }
    }
}