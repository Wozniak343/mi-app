import { Component, signal, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';

interface TareaRow {
  id: number;
  titulo: string;
  descripcion: string | null;
  estado: boolean;
  fechaCreacion: string;
  fechaVencimiento: string | null;
}

interface CrearTareaRequest {
  titulo: string;
  descripcion?: string | null;
  fechaVencimiento?: string | null;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('mi-app');
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);

  tareasUsuarios = signal<TareaRow[]>([]);
  mostrarFormulario = signal(false);
  cargando = signal(false);
  errorMessage = signal<string | null>(null);
  searchTerm = signal('');
  searchState = signal<string>('');

  tareaForm: FormGroup;

  constructor() {
    this.tareaForm = this.fb.group({
      titulo: ['', [Validators.required, Validators.minLength(3)]],
      descripcion: [''],
      fechaVencimiento: ['']
    });
  }

  ngOnInit() {
    this.cargarTareasUsuarios();
  }

  cargarTareasUsuarios() {
    this.cargando.set(true);
  this.http.get<TareaRow[]>('/api/tareas-usuarios')
      .subscribe({
        next: (data) => {
          this.tareasUsuarios.set(data);
          this.cargando.set(false);
          console.log('Tareas y usuarios cargados:', data);
        },
        error: (error) => {
          console.error('Error al cargar tareas y usuarios:', error);
          this.cargando.set(false);
        }
      });
  }

  buscarTarea() {
    const q = this.searchTerm().trim();
    if (!q) {
      // if only estado is set, still fetch with estado filter
      if (this.searchState()) {
        // trigger fetch by estado only
        const stateVal = this.searchState() === 'true' ? 'true' : 'false';
        const urlOnly = `/api/tareas-usuarios?estado=${stateVal}`;
        this.cargando.set(true);
        this.http.get<TareaRow[]>(urlOnly).subscribe({ next: (data) => { this.tareasUsuarios.set(data); this.cargando.set(false); }, error: (err) => { console.error(err); this.errorMessage.set('Error al buscar la tarea'); this.cargando.set(false); } });
        return;
      }
      this.cargarTareasUsuarios();
      return;
    }

    this.cargando.set(true);
    let url = `/api/tareas-usuarios?titulo=${encodeURIComponent(q)}`;
    if (this.searchState()) {
      url += `&estado=${this.searchState()}`;
    }
    this.http.get<TareaRow[]>(url).subscribe({
      next: (data) => {
        this.tareasUsuarios.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        console.error('Error buscando tarea:', err);
        this.errorMessage.set('Error al buscar la tarea');
        this.cargando.set(false);
      }
    });
  }

  clearSearch() {
    this.searchTerm.set('');
    this.cargarTareasUsuarios();
  }

  onSearchInput(event: Event) {
    const v = (event.target as HTMLInputElement).value;
    this.searchTerm.set(v);
  }

  onStateChange(event: Event) {
    const v = (event.target as HTMLSelectElement).value;
    this.searchState.set(v);
  }

  toggleFormulario() {
    this.mostrarFormulario.set(!this.mostrarFormulario());
    if (!this.mostrarFormulario()) {
      this.tareaForm.reset();
    }
  }

  closeError() {
    this.errorMessage.set(null);
  }

  crearTarea() {
    if (this.tareaForm.valid) {
      this.cargando.set(true);
      const formData = this.tareaForm.value as CrearTareaRequest;

      // Client-side validation: fechaVencimiento must be >= today if provided
      if (formData.fechaVencimiento) {
        const picked = new Date(formData.fechaVencimiento);
        // normalize to date-only
        const pickedDate = new Date(picked.getFullYear(), picked.getMonth(), picked.getDate());
        const today = new Date();
        const todayDate = new Date(today.getFullYear(), today.getMonth(), today.getDate());
        if (pickedDate < todayDate) {
          // mark control as invalid
          this.tareaForm.get('fechaVencimiento')?.setErrors({ minDate: true });
          this.cargando.set(false);
          return;
        }
      }

      this.http.post<TareaRow>('/api/tareas-usuarios', formData)
        .subscribe({
          next: (created) => {
            console.log('Tarea creada:', created);
            // Insert the created row into the UI immediately
            const current = this.tareasUsuarios();
            this.tareasUsuarios.set([...(current ?? []), created]);
            this.tareaForm.reset();
            this.mostrarFormulario.set(false);
            this.cargando.set(false);
            this.errorMessage.set(null);
          },
          error: (error) => {
            console.error('Error al crear tarea:', error);
            // Try to extract the server-sent message body, fallback to generic
            let msg = 'Error al crear la tarea';
            try {
              // Angular's HttpErrorResponse: error.error may contain the body
              const body = (error as any).error;
              if (body && body.error) msg = body.error;
              else if (body && body.title) msg = body.title;
              else if ((error as any).message) msg = (error as any).message;
            } catch {
              /* ignore */
            }
            this.errorMessage.set(msg);
            this.cargando.set(false);
          }
        });
    } else {
      // Marcar todos los campos como tocados para mostrar errores
      Object.keys(this.tareaForm.controls).forEach(key => {
        this.tareaForm.get(key)?.markAsTouched();
      });
    }
  }
}
