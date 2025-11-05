import { Component, signal, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

interface TareaUsuario {
  nombre: string;
  email: string;
  titulo: string;
  completada: boolean;
  fechaVencimiento: string | null;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('mi-app');
  private http = inject(HttpClient);
  tareasUsuarios = signal<TareaUsuario[]>([]);

  ngOnInit() {
    this.cargarTareasUsuarios();
  }

  cargarTareasUsuarios() {
    this.http.get<TareaUsuario[]>('/api/tareas-usuarios')
      .subscribe({
        next: (data) => {
          this.tareasUsuarios.set(data);
          console.log('Tareas y usuarios cargados:', data);
        },
        error: (error) => {
          console.error('Error al cargar tareas y usuarios:', error);
        }
      });
  }
}
