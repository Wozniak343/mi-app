import { Component, signal, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';

interface TareaUsuario {
  id: number;
  nombre: string;
  email: string;
  titulo: string;
  completada: boolean;
  fechaVencimiento: string | null;
}

interface CrearTareaRequest {
  nombre: string;
  email: string;
  titulo: string;
  descripcion?: string;
  fechaVencimiento?: string;
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

  tareasUsuarios = signal<TareaUsuario[]>([]);
  mostrarFormulario = signal(false);
  cargando = signal(false);

  tareaForm: FormGroup;

  constructor() {
    this.tareaForm = this.fb.group({
      nombre: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
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
    this.http.get<TareaUsuario[]>('/api/tareas-usuarios')
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

      this.http.post('/api/crear-tarea', formData)
        .subscribe({
          next: (response) => {
            console.log('Tarea creada:', response);
            this.tareaForm.reset();
            this.mostrarFormulario.set(false);
            this.cargarTareasUsuarios(); // Recargar la tabla
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
