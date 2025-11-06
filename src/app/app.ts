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

  tareaForm: FormGroup;

  constructor() {
    this.tareaForm = this.fb.group({
      titulo: ['', [Validators.required, Validators.minLength(3)]],
      descripcion: ['']
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

  toggleFormulario() {
    this.mostrarFormulario.set(!this.mostrarFormulario());
    if (!this.mostrarFormulario()) {
      this.tareaForm.reset();
    }
  }

  crearTarea() {
    if (this.tareaForm.valid) {
      this.cargando.set(true);
      const formData = this.tareaForm.value as CrearTareaRequest;

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
          },
          error: (error) => {
            console.error('Error al crear tarea:', error);
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
